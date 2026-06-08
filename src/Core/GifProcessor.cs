using FFMpegCore;
using FFMpegCore.Exceptions;
using FFMpegCore.Pipes;
using ImageMagick;
using ImageMagick.Drawing;
using SteamGifCropper;
using SteamGifCropper.Properties;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GifProcessorApp
{
    public static partial class GifProcessor
    {
        private static readonly (int Start, int End)[] Ranges766 = { (0, 149), (154, 303), (308, 457), (462, 611), (616, 765) };
        private static readonly (int Start, int End)[] Ranges774 = { (0, 149), (155, 305), (311, 461), (467, 617), (623, 773) };
        private const int HeightExtension = 100;
        private const uint SupportedWidth1 = 766;
        private const uint SupportedWidth2 = 774;

        private static readonly int FfmpegTimeoutSeconds = GetAppSettingInt("FFmpeg.TimeoutSeconds", 300);
        private static readonly int FfmpegThreads = GetAppSettingInt("FFmpeg.Threads", 0);

        private static bool IsValidCanvasWidth(uint width) => width == SupportedWidth1 || width == SupportedWidth2;

        private static void ShowUnsupportedWidthError(uint width)
        {
            string message = string.Format(SteamGifCropper.Properties.Resources.Error_UnsupportedWidth, width, SupportedWidth1, SupportedWidth2);
            WindowsThemeManager.ShowThemeAwareMessageBox(null, message, SteamGifCropper.Properties.Resources.Title_Error, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private static (int Start, int End)[] GetCropRanges(uint canvasWidth)
        {
            return canvasWidth == SupportedWidth1 ? Ranges766 : Ranges774;
        }

        private static int GetAppSettingInt(string key, int defaultValue)
        {
            try
            {
                var value = ConfigurationManager.AppSettings[key];
                if (!string.IsNullOrEmpty(value) && int.TryParse(value, out var result))
                {
                    return result;
                }
            }
            catch (Exception ex)
            {
                // Log configuration read error but use default value
                System.Diagnostics.Debug.WriteLine($"Failed to read app setting '{key}': {ex.Message}");
            }
            return defaultValue;
        }

        private static CancellationToken CreateFfmpegCancellationToken()
        {
            return FfmpegTimeoutSeconds > 0
                ? new CancellationTokenSource(TimeSpan.FromSeconds(FfmpegTimeoutSeconds)).Token
                : CancellationToken.None;
        }

        private static void ApplyThreadLimit(FFMpegArgumentOptions options)
        {
            if (FfmpegThreads > 0)
            {
                options.WithCustomArgument($"-threads {FfmpegThreads}");
            }
        }

        private const int ProgressUpdateInterval = 10;
        private static int _lastProgressFrame = -ProgressUpdateInterval;

        public static void SetProgressBar(ProgressBar progressBar, int current, int total)
        {
            if (progressBar == null || total <= 0) return;

            void UpdateUI()
            {
                progressBar.Minimum = 0;
                progressBar.Maximum = total;
                progressBar.Value = Math.Max(progressBar.Minimum, Math.Min(current, total));
            }

            if (progressBar.InvokeRequired)
            {
                progressBar.BeginInvoke((Action)UpdateUI);
            }
            else
            {
                UpdateUI();
            }
        }

        // Marshaled setters for the progress bar range/visibility. The heavy operations now run on a
        // background thread (Task.Run), so direct writes to pBarTaskStatus.Minimum/Maximum/Visible would
        // be cross-thread violations; these route through BeginInvoke just like SetProgressBar.
        public static void SetProgressRange(GifToolMainForm mainForm, int minimum, int maximum)
        {
            if (mainForm == null) return;

            void UpdateUI()
            {
                mainForm.pBarTaskStatus.Minimum = minimum;
                mainForm.pBarTaskStatus.Maximum = Math.Max(minimum + 1, maximum);
            }

            if (mainForm.InvokeRequired)
            {
                mainForm.BeginInvoke((Action)UpdateUI);
            }
            else
            {
                UpdateUI();
            }
        }

        public static void SetProgressVisible(GifToolMainForm mainForm, bool visible)
        {
            if (mainForm == null) return;

            void UpdateUI() => mainForm.pBarTaskStatus.Visible = visible;

            if (mainForm.InvokeRequired)
            {
                mainForm.BeginInvoke((Action)UpdateUI);
            }
            else
            {
                UpdateUI();
            }
        }

        public static void SetStatusText(GifToolMainForm mainForm, string text)
        {
            if (mainForm == null) return;

            void UpdateUI() => mainForm.lblStatus.Text = text;

            if (mainForm.InvokeRequired)
            {
                mainForm.BeginInvoke((Action)UpdateUI);
            }
            else
            {
                UpdateUI();
            }
        }

        // Immutable snapshot of the gifsicle UI settings. Captured ONCE on the UI thread before any
        // Task.Run, so the background worker never touches the controls (which would throw cross-thread).
        private struct GifsicleSnapshot
        {
            public bool Enabled;
            public int Colors;
            public int Lossy;
            public int OptimizeLevel;
            public int Dither;
            public int ThresholdKB;
            public long ThresholdBytes;
            public int TimeoutSeconds;
            // Auto-fit (split-flow): when AutoFit is set, each part is shrunk to <= TargetBytes via
            // GifSizeFitter instead of a single gifsicle pass.
            public bool AutoFit;
            public int TargetKB;
            public long TargetBytes;
            public int FitMaxAttempts;
        }

        // MUST be called on the UI thread. useOverride=true takes Enabled from enabledOverride instead
        // of chkGifsicle (used by ConcatenateGifs, which gates on its own settings flag).
        private static GifsicleSnapshot CaptureGifsicleSnapshot(GifToolMainForm mainForm, bool enabledOverride, bool useOverride)
        {
            int kb = (int)mainForm.numUpDownGifsicleMinKB.Value;
            int targetKb = (int)mainForm.numUpDownTargetKB.Value;
            return new GifsicleSnapshot
            {
                Enabled = useOverride ? enabledOverride : mainForm.chkGifsicle.Checked,
                Colors = (int)mainForm.numUpDownPaletteSicle.Value,
                Lossy = (int)mainForm.numUpDownLossy.Value,
                OptimizeLevel = (int)mainForm.numUpDownOptimize.Value,
                Dither = mainForm.DitherMethod,
                ThresholdKB = kb,
                ThresholdBytes = (long)kb * 1024L,
                TimeoutSeconds = (int)mainForm.numUpDownGifsicleTimeout.Value,
                AutoFit = mainForm.chkAutoFitSplit.Checked,
                TargetKB = targetKb,
                TargetBytes = (long)targetKb * 1024L,
                FitMaxAttempts = (int)mainForm.numUpDownFitAttempts.Value
            };
        }

        private static GifsicleSnapshot CaptureGifsicleSnapshot(GifToolMainForm mainForm)
            => CaptureGifsicleSnapshot(mainForm, false, false);

        // Background-safe: reads no controls. Runs gifsicle only when enabled AND the file is strictly
        // larger than the configured KB threshold; otherwise reports a "skipped" status and returns.
        private static async Task OptimizeWithGifsicleIfEnabled(GifToolMainForm mainForm, GifsicleSnapshot snapshot, string path, IProgress<int> progress = null)
        {
            if (!snapshot.Enabled) return;

            long size = new FileInfo(path).Length;
            if (size <= snapshot.ThresholdBytes)
            {
                SetStatusText(mainForm, string.Format(Resources.Status_GifsicleSkippedBelowThreshold, snapshot.ThresholdKB));
                return;
            }

            SetStatusText(mainForm, Resources.Status_GifsicleOptimizing);
            if (snapshot.TimeoutSeconds > 0)
            {
                GifsicleWrapper.ProcessTimeout = TimeSpan.FromSeconds(snapshot.TimeoutSeconds);
            }
            var options = new GifsicleWrapper.GifsicleOptions
            {
                Colors = snapshot.Colors,
                Lossy = snapshot.Lossy,
                OptimizeLevel = snapshot.OptimizeLevel,
                Dither = snapshot.Dither
            };
            await GifsicleWrapper.OptimizeGif(path, path, options, progress);
        }

        // Standalone gifsicle: optimize a single user-chosen GIF using the panel settings
        // (Lossy/Palette/Optimize/Dither/timeout). Unlike the auto path it ignores the chkGifsicle
        // gate and the size threshold — running it is an explicit user action. Writes a *_gifsicle.gif.
        public static async Task OptimizeSingleGif(GifToolMainForm mainForm)
        {
            using var openFileDialog = new OpenFileDialog
            {
                Filter = SteamGifCropper.Properties.Resources.FileDialog_GifFilter,
                Title = SteamGifCropper.Properties.Resources.FileDialog_SelectGif
            };
            if (openFileDialog.ShowDialog() != DialogResult.OK) return;

            string inputPath = openFileDialog.FileName;
            ImageInputValidator.ValidateGif(inputPath);
            string outputPath = GenerateOutputPath(inputPath, "_gifsicle");

            // Read panel settings on the UI thread.
            var options = new GifsicleWrapper.GifsicleOptions
            {
                Colors = (int)mainForm.numUpDownPaletteSicle.Value,
                Lossy = (int)mainForm.numUpDownLossy.Value,
                OptimizeLevel = (int)mainForm.numUpDownOptimize.Value,
                Dither = mainForm.DitherMethod
            };
            int timeout = (int)mainForm.numUpDownGifsicleTimeout.Value;

            mainForm.Enabled = false;
            SetProgressVisible(mainForm, true);
            SetProgressRange(mainForm, 0, 100);
            SetProgressBar(mainForm.pBarTaskStatus, 0, 100);
            try
            {
                SetStatusText(mainForm, SteamGifCropper.Properties.Resources.Status_GifsicleOptimizing);
                if (timeout > 0)
                {
                    GifsicleWrapper.ProcessTimeout = TimeSpan.FromSeconds(timeout);
                }

                var progress = new Progress<int>(p =>
                {
                    SetProgressBar(mainForm.pBarTaskStatus, p, 100);
                    SetStatusText(mainForm, $"{SteamGifCropper.Properties.Resources.Status_GifsicleOptimizing} ({p}%)");
                });
                await GifsicleWrapper.OptimizeGif(inputPath, outputPath, options, progress);

                SetProgressBar(mainForm.pBarTaskStatus, 100, 100);
                SetStatusText(mainForm, SteamGifCropper.Properties.Resources.Status_Done);
                WindowsThemeManager.ShowThemeAwareMessageBox(mainForm,
                    SteamGifCropper.Properties.Resources.Message_ProcessingComplete + "\n" + Path.GetFileName(outputPath),
                    SteamGifCropper.Properties.Resources.Title_Success, MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                SetStatusText(mainForm, SteamGifCropper.Properties.Resources.Status_Error);
                WindowsThemeManager.ShowThemeAwareMessageBox(mainForm,
                    string.Format(SteamGifCropper.Properties.Resources.Error_Occurred, ex.Message),
                    SteamGifCropper.Properties.Resources.Title_Error, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                mainForm.Enabled = true;
                SetProgressBar(mainForm.pBarTaskStatus, 0, 100);
                SetStatusText(mainForm, SteamGifCropper.Properties.Resources.Status_Idle);
            }
        }

        // ── Auto-fit to target size ──────────────────────────────────────────────────────────
        // A single gifsicle pass often can't get a Steam part under the ~5 MB limit, so these
        // search the (colors, lossy) space with GifSizeFitter, running gifsicle entirely in memory
        // (stdin/stdout, no temp files) against the source bytes. See GifSizeFitter for the
        // quality-first strategy (max colors, then min lossy under the size budget).

        // Engine glue: in-memory gifsicle as the fit runner; maps per-attempt progress to 0-100.
        private static Task<GifSizeFitter.FitResult> AutoFitBytes(
            byte[] source, long targetBytes, int maxAttempts, int optimizeLevel, int dither,
            IProgress<int> progress = null)
        {
            var fitOptions = new GifSizeFitter.FitOptions
            {
                TargetBytes = targetBytes,
                MaxAttempts = maxAttempts,
                OptimizeLevel = optimizeLevel,
                Dither = dither
            };
            IProgress<(int attempt, int maxAttempts, long bytes, string status)> fitProgress = progress == null
                ? null
                : new Progress<(int attempt, int maxAttempts, long bytes, string status)>(p =>
                    progress.Report(Math.Min(100, p.attempt * 100 / Math.Max(1, p.maxAttempts))));

            return GifSizeFitter.FitAsync(source, fitOptions,
                (bytes, opts) => GifsicleWrapper.OptimizeGifInMemory(bytes, opts), fitProgress);
        }

        // Split-flow integration: auto-fit one freshly-written part in place to the snapshot target.
        // Parts already at/under target are left untouched. Runs before ModifyGifFile, which then
        // re-applies the Steam header/tail exactly as in the single-pass path.
        private static async Task AutoFitFileInPlace(GifToolMainForm mainForm, GifsicleSnapshot gifsicle, string path, IProgress<int> progress = null)
        {
            byte[] source = File.ReadAllBytes(path);
            if (source.LongLength <= gifsicle.TargetBytes)
            {
                SetStatusText(mainForm, string.Format(Resources.Status_AutoFitAlreadyUnder, gifsicle.TargetKB));
                return;
            }

            if (gifsicle.TimeoutSeconds > 0)
            {
                GifsicleWrapper.ProcessTimeout = TimeSpan.FromSeconds(gifsicle.TimeoutSeconds);
            }

            var result = await AutoFitBytes(source, gifsicle.TargetBytes, gifsicle.FitMaxAttempts,
                gifsicle.OptimizeLevel, gifsicle.Dither, progress);

            if (result.Data != null && result.Bytes < source.LongLength)
            {
                File.WriteAllBytes(path, result.Data);
            }

            SetStatusText(mainForm, string.Format(
                result.TargetMet ? Resources.Status_AutoFitDone : Resources.Status_AutoFitOverTarget,
                result.Bytes / 1024, result.Colors, result.Lossy));
        }

        // Standalone batch: pick one or more GIFs and shrink each to <= the panel target, writing
        // *_fit.gif (non-destructive). If a source carried the Steam tail byte (0x21), the fitted
        // output keeps it — gifsicle preserves the faked header height and only normalizes the tail
        // to 0x3B, which we flip back so already-split parts stay Steam-ready.
        public static async Task AutoFitGifsToSize(GifToolMainForm mainForm)
        {
            using var openFileDialog = new OpenFileDialog
            {
                Filter = Resources.FileDialog_GifFilter,
                Title = Resources.Button_AutoFitSize,
                Multiselect = true
            };
            if (openFileDialog.ShowDialog() != DialogResult.OK) return;

            string[] files = openFileDialog.FileNames;

            // Read panel settings on the UI thread.
            int targetKB = (int)mainForm.numUpDownTargetKB.Value;
            long targetBytes = (long)targetKB * 1024L;
            int maxAttempts = (int)mainForm.numUpDownFitAttempts.Value;
            int optimize = (int)mainForm.numUpDownOptimize.Value;
            int dither = mainForm.DitherMethod;
            int timeout = (int)mainForm.numUpDownGifsicleTimeout.Value;

            mainForm.Enabled = false;
            SetProgressVisible(mainForm, true);
            SetProgressRange(mainForm, 0, 100);
            SetProgressBar(mainForm.pBarTaskStatus, 0, 100);

            var report = new System.Text.StringBuilder();
            try
            {
                if (timeout > 0)
                {
                    GifsicleWrapper.ProcessTimeout = TimeSpan.FromSeconds(timeout);
                }

                for (int i = 0; i < files.Length; i++)
                {
                    string inputPath = files[i];
                    ImageInputValidator.ValidateGif(inputPath);
                    string outputPath = GenerateOutputPath(inputPath, "_fit");

                    byte[] source = File.ReadAllBytes(inputPath);
                    long origKB = source.LongLength / 1024;
                    bool steamTail = source.Length > 0 && source[source.Length - 1] == 0x21;

                    int fileIndex = i;
                    int span = Math.Max(1, 100 / files.Length);
                    int basePct = fileIndex * span;
                    var progress = new Progress<int>(p =>
                    {
                        SetProgressBar(mainForm.pBarTaskStatus, Math.Min(100, basePct + p * span / 100), 100);
                        SetStatusText(mainForm, string.Format(Resources.Status_AutoFitProgress, fileIndex + 1, files.Length, p));
                    });

                    if (source.LongLength <= targetBytes)
                    {
                        File.WriteAllBytes(outputPath, source);
                        report.AppendLine($"{Path.GetFileName(inputPath)}: {origKB} KB (already <= {targetKB} KB)");
                        continue;
                    }

                    var result = await AutoFitBytes(source, targetBytes, maxAttempts, optimize, dither, progress);
                    byte[] outData = result.Data;
                    if (steamTail && outData.Length > 0 && outData[outData.Length - 1] == 0x3B)
                    {
                        outData[outData.Length - 1] = 0x21; // preserve the Steam tail byte
                    }
                    File.WriteAllBytes(outputPath, outData);

                    report.AppendLine($"{Path.GetFileName(inputPath)}: {origKB} -> {result.Bytes / 1024} KB " +
                        $"(colors {result.Colors}, lossy {result.Lossy}){(result.TargetMet ? "" : "  ** still over target **")}");
                }

                SetProgressBar(mainForm.pBarTaskStatus, 100, 100);
                SetStatusText(mainForm, Resources.Status_Done);
                WindowsThemeManager.ShowThemeAwareMessageBox(mainForm, report.ToString(),
                    Resources.Title_Success, MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                SetStatusText(mainForm, Resources.Status_Error);
                WindowsThemeManager.ShowThemeAwareMessageBox(mainForm,
                    string.Format(Resources.Error_Occurred, ex.Message),
                    Resources.Title_Error, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                mainForm.Enabled = true;
                SetProgressBar(mainForm.pBarTaskStatus, 0, 100);
                SetStatusText(mainForm, Resources.Status_Idle);
            }
        }

        private static void UpdateFrameProgress(GifToolMainForm mainForm, int currentFrame, int totalFrames)
        {
            if (totalFrames <= 0) return;

            // _lastProgressFrame is static and otherwise retains the previous operation's last
            // value. When the next operation has fewer frames, every update would be throttled
            // away until the final frame (progress bar appears frozen). Reset when a new
            // operation starts (frame count restarts at 1 or runs backwards).
            if (currentFrame <= 1 || currentFrame < _lastProgressFrame)
            {
                _lastProgressFrame = -ProgressUpdateInterval;
            }

            if (currentFrame - _lastProgressFrame < ProgressUpdateInterval && currentFrame != totalFrames)
            {
                return;
            }
            _lastProgressFrame = currentFrame;

            void UpdateUI()
            {
                int percent = Math.Min((int)((double)currentFrame / totalFrames * 100), 100);
                SetProgressBar(mainForm.pBarTaskStatus, percent, 100);
                SetStatusText(mainForm, $"{currentFrame}/{totalFrames} ({percent}%)");
            }

            if (mainForm.InvokeRequired)
            {
                mainForm.BeginInvoke((Action)UpdateUI);
            }
            else
            {
                UpdateUI();
            }
        }

        private static void UpdateFrameProgressByFrame(GifToolMainForm mainForm, int currentFrame, int totalFrames)
        {
            if (mainForm == null || totalFrames <= 0) return;

            void UpdateUI()
            {
                SetProgressBar(mainForm.pBarTaskStatus, currentFrame, totalFrames);
                int percent = Math.Min((int)((double)currentFrame / totalFrames * 100), 100);
                SetStatusText(mainForm, $"{currentFrame}/{totalFrames} ({percent}%)");
            }

            if (mainForm.InvokeRequired)
            {
                mainForm.BeginInvoke((Action)UpdateUI);
            }
            else
            {
                UpdateUI();
            }
        }

    }
}
