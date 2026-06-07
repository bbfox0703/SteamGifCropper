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
    public static class GifProcessor
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
        }

        // MUST be called on the UI thread. useOverride=true takes Enabled from enabledOverride instead
        // of chkGifsicle (used by ConcatenateGifs, which gates on its own settings flag).
        private static GifsicleSnapshot CaptureGifsicleSnapshot(GifToolMainForm mainForm, bool enabledOverride, bool useOverride)
        {
            int kb = (int)mainForm.numUpDownGifsicleMinKB.Value;
            return new GifsicleSnapshot
            {
                Enabled = useOverride ? enabledOverride : mainForm.chkGifsicle.Checked,
                Colors = (int)mainForm.numUpDownPaletteSicle.Value,
                Lossy = (int)mainForm.numUpDownLossy.Value,
                OptimizeLevel = (int)mainForm.numUpDownOptimize.Value,
                Dither = mainForm.DitherMethod,
                ThresholdKB = kb,
                ThresholdBytes = (long)kb * 1024L
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
            var options = new GifsicleWrapper.GifsicleOptions
            {
                Colors = snapshot.Colors,
                Lossy = snapshot.Lossy,
                OptimizeLevel = snapshot.OptimizeLevel,
                Dither = snapshot.Dither
            };
            await GifsicleWrapper.OptimizeGif(path, path, options, progress);
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

        public static async Task StartProcessing(GifToolMainForm mainForm)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = SteamGifCropper.Properties.Resources.FileDialog_GifFilter,
                Title = SteamGifCropper.Properties.Resources.FileDialog_SelectGif
            };

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                string inputFilePath = openFileDialog.FileName;
                ImageInputValidator.ValidateGif(inputFilePath);
                SetStatusText(mainForm, "Split GIF...");
                try
                {
                    using (var collection = new MagickImageCollection(inputFilePath))
                    {
                        uint canvasWidth = collection[0].Page.Width;
                        uint canvasHeight = collection[0].Page.Height;

                        if (!IsValidCanvasWidth(canvasWidth))
                        {
                            ShowUnsupportedWidthError(canvasWidth);
                            return;
                        }

                        var gifsicle = CaptureGifsicleSnapshot(mainForm);
                        mainForm.Enabled = false;
                        SetProgressRange(mainForm, 0, 100);
                        SetProgressBar(mainForm.pBarTaskStatus, 0, 100);
                        SetStatusText(mainForm, SteamGifCropper.Properties.Resources.Status_Processing);

                        var ranges = GetCropRanges(canvasWidth);

                        await SplitGif(inputFilePath, mainForm, ranges, (int)canvasHeight, gifsicle);
                        SetStatusText(mainForm, SteamGifCropper.Properties.Resources.Status_Done);
                        WindowsThemeManager.ShowThemeAwareMessageBox(mainForm,
                                        SteamGifCropper.Properties.Resources.Message_ProcessingComplete,
                                        SteamGifCropper.Properties.Resources.Title_Success, MessageBoxButtons.OK, MessageBoxIcon.Information);

                    }
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
                    SetProgressBar(mainForm.pBarTaskStatus, 0, mainForm.pBarTaskStatus.Maximum);
                    SetStatusText(mainForm, SteamGifCropper.Properties.Resources.Status_Idle);
                }
            }
        }

        // Draws a grid/lattice aligned to the Steam showcase slots over a 766/774 GIF, turning the
        // forced 4px/6px slot gaps into a deliberate mosaic. Outputs a single full-width GIF (no split)
        // so it can be chained; split later with the main "Split GIF" button.
        public static async Task GridMosaic(GifToolMainForm mainForm)
        {
            using (var dialog = new GridMosaicDialog())
            {
                if (dialog.ShowDialog() != DialogResult.OK)
                {
                    return;
                }

                string inputFilePath = dialog.InputFilePath;
                ImageInputValidator.ValidateGif(inputFilePath);

                uint canvasWidth;
                using (var probe = new MagickImageCollection(inputFilePath))
                {
                    canvasWidth = probe[0].Page.Width;
                }
                if (!IsValidCanvasWidth(canvasWidth))
                {
                    ShowUnsupportedWidthError(canvasWidth);
                    return;
                }

                var grid = new GridMosaicSettings
                {
                    InputFilePath = inputFilePath,
                    ColumnsPerSlot = dialog.ColumnsPerSlot,
                    Rows = dialog.Rows,
                    LineWidth = dialog.LineWidth,
                    Style = dialog.Style,
                    LineColor = dialog.LineColor
                };
                string outputFilePath = GenerateOutputPath(inputFilePath, "_grid");
                var gifsicle = CaptureGifsicleSnapshot(mainForm);

                mainForm.Enabled = false;
                SetProgressRange(mainForm, 0, 100);
                SetProgressBar(mainForm.pBarTaskStatus, 0, 100);
                SetStatusText(mainForm, SteamGifCropper.Properties.Resources.Status_Processing);
                try
                {
                    await ApplyGridMosaic(mainForm, inputFilePath, outputFilePath, grid, gifsicle);

                    SetProgressBar(mainForm.pBarTaskStatus, 100, 100);
                    SetStatusText(mainForm, SteamGifCropper.Properties.Resources.Status_Done);
                    WindowsThemeManager.ShowThemeAwareMessageBox(mainForm,
                                    SteamGifCropper.Properties.Resources.Message_ProcessingComplete,
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
        }

        // Overlays the slot-aligned grid onto every frame of the full-width GIF (no split). The grid is
        // built once per slot and composited at each slot's x-offset, so the result matches the old
        // per-part appearance (gaps stay grid-free). Runs on a background thread.
        private static async Task ApplyGridMosaic(GifToolMainForm mainForm, string inputFilePath,
            string outputFilePath, GridMosaicSettings grid, GifsicleSnapshot gifsicle)
        {
            await Task.Run(() =>
            {
                SetStatusText(mainForm, SteamGifCropper.Properties.Resources.Status_CoalescingFrames);
                using var collection = new MagickImageCollection(inputFilePath);
                collection.Coalesce();

                uint canvasWidth = collection[0].Width;
                int canvasHeight = (int)collection[0].Height;
                var ranges = GetCropRanges(canvasWidth);

                var gridLayers = new MagickImage[ranges.Length];
                try
                {
                    for (int c = 0; c < ranges.Length; c++)
                    {
                        int partWidth = ranges[c].End - ranges[c].Start + 1;
                        gridLayers[c] = GridMosaicRenderer.BuildGridLayer((uint)partWidth, (uint)canvasHeight, canvasHeight, grid);
                    }

                    var op = grid.Style == GridLineStyle.Transparent ? CompositeOperator.DstOut : CompositeOperator.Over;
                    int total = collection.Count;
                    int idx = 0;
                    foreach (var frame in collection)
                    {
                        for (int c = 0; c < ranges.Length; c++)
                        {
                            frame.Composite(gridLayers[c], ranges[c].Start, 0, op);
                        }
                        frame.Settings.SetDefine("compress", "LZW");

                        idx++;
                        if (idx % 10 == 0 || idx == total)
                        {
                            SetProgressBar(mainForm.pBarTaskStatus, idx * 90 / total, 100);
                            SetStatusText(mainForm, SteamGifCropper.Properties.Resources.Status_Processing);
                        }
                    }

                    SetStatusText(mainForm, SteamGifCropper.Properties.Resources.Status_Optimizing);
                    collection.Optimize();
                    SetStatusText(mainForm, SteamGifCropper.Properties.Resources.Status_Saving);
                    collection.Write(outputFilePath);
                }
                finally
                {
                    foreach (var layer in gridLayers)
                    {
                        layer?.Dispose();
                    }
                }
            });

            await OptimizeWithGifsicleIfEnabled(mainForm, gifsicle, outputFilePath);
        }


        private static (int[] delays, int ticksPerSecond) RecalculateGifDelays(MagickImageCollection collection)
        {
            int sourceTicks = (int)collection[0].AnimationTicksPerSecond;
            if (sourceTicks <= 0)
            {
                sourceTicks = 100;
            }

            // Always preserve original frame delays and timing
            var originalDelays = collection.Select(f => (int)f.AnimationDelay).ToArray();
            return (originalDelays, sourceTicks);
        }

        private static async Task SplitGif(string inputFilePath, GifToolMainForm mainForm, (int Start, int End)[] ranges, int canvasHeight, GifsicleSnapshot gifsicle, GridMosaicSettings grid = null)
        {
            // All heavy ImageMagick work runs on a background thread so the UI stays responsive.
            // UI updates go exclusively through the marshaled SetProgressBar/SetStatusText/SetProgress*
            // helpers; no direct control access happens inside this lambda.
            await Task.Run(async () =>
            {
                SetStatusText(mainForm, SteamGifCropper.Properties.Resources.Status_CoalescingFrames);
                using var collection = new MagickImageCollection(inputFilePath);
                collection.Coalesce();
                int newHeight = canvasHeight + HeightExtension;

                var (recalculatedDelays, ticksPerSecond) = RecalculateGifDelays(collection);
                collection[0].AnimationTicksPerSecond = ticksPerSecond;

                int totalFrames = collection.Count * ranges.Length;
                int currentFrame = 0;

                for (int i = 0; i < ranges.Length; i++)
                {
                    int partWidth = ranges[i].End - ranges[i].Start + 1;
                    MagickImage gridLayer = grid != null
                        ? GridMosaicRenderer.BuildGridLayer((uint)partWidth, (uint)newHeight, canvasHeight, grid)
                        : null;
                    try
                    {
                    using (var partCollection = new MagickImageCollection())
                    {
                        for (int frameIndex = 0; frameIndex < collection.Count; frameIndex++)
                        {
                            var frame = collection[frameIndex];
                            int copyWidth = ranges[i].End - ranges[i].Start + 1;

                            if (currentFrame % ProgressUpdateInterval == 0)
                            {
                                SetStatusText(mainForm, string.Format("Splitting part {0}/5 - Frame {1}/{2}", i + 1, (currentFrame % collection.Count) + 1, collection.Count));
                            }

                            MagickImage newImage = null;
                            try
                            {
                                newImage = new MagickImage(MagickColors.Transparent, (uint)copyWidth, (uint)newHeight);

                                var cropGeometry = new MagickGeometry(ranges[i].Start, 0, (uint)copyWidth, (uint)canvasHeight);
                                using (var croppedFrame = frame.Clone())
                                {
                                    croppedFrame.Crop(cropGeometry);
                                    croppedFrame.ResetPage();
                                    newImage.Composite(croppedFrame, 0, 0, CompositeOperator.Over);
                                }

                                if (gridLayer != null)
                                {
                                    GridMosaicRenderer.ApplyGridLayer(newImage, gridLayer, grid.Style);
                                }

                                newImage.AnimationDelay = (uint)recalculatedDelays[frameIndex];
                                newImage.AnimationTicksPerSecond = ticksPerSecond;
                                newImage.GifDisposeMethod = GifDisposeMethod.Background;

                                partCollection.Add(newImage);
                                newImage = null; // Ownership transferred to collection
                            }
                            finally
                            {
                                // Dispose only if not added to collection
                                newImage?.Dispose();
                            }

                            currentFrame++;
                            UpdateFrameProgress(mainForm, currentFrame, totalFrames);
                        }

                        string outputFile = $"{Path.GetFileNameWithoutExtension(inputFilePath)}_Part{i + 1}.gif";
                        string outputDir = Path.GetDirectoryName(inputFilePath);
                        string outputPath = Path.Combine(outputDir, outputFile);

                        partCollection.Optimize();
                        partCollection[0].AnimationTicksPerSecond = ticksPerSecond;
                        SetStatusText(mainForm, SteamGifCropper.Properties.Resources.Status_Compressing);
                        int compressFrameCount = 0;
                        foreach (var frame in partCollection)
                        {
                            frame.Settings.SetDefine("compress", "LZW");
                            frame.Settings.SetDefine(MagickFormat.Gif, "optimize-transparency", "true");

                            if (++compressFrameCount % 25 == 0)
                            {
                                SetStatusText(mainForm, SteamGifCropper.Properties.Resources.Status_Compressing);
                            }
                        }

                        SetStatusText(mainForm, SteamGifCropper.Properties.Resources.Status_Saving);

                        SetProgressVisible(mainForm, true);

                        partCollection.Write(outputPath);

                        // Keep the overall progress monotonic: each part owns one band of the bar
                        // (e.g. 0-20-40-60-80-100 for 5 parts) instead of spiking to 100% per part,
                        // which made the themed progress bar sweep to the far right on every part.
                        int partSpan = 100 / ranges.Length;
                        int partBase = i * partSpan;

                        var progress = new Progress<int>(p =>
                        {
                            SetProgressBar(mainForm.pBarTaskStatus, partBase + p * partSpan / 100, 100);
                            SetStatusText(mainForm, $"{SteamGifCropper.Properties.Resources.Status_GifsicleOptimizing} ({p}%)");
                        });

                        await OptimizeWithGifsicleIfEnabled(mainForm, gifsicle, outputPath, progress);

                        // Finalize this part's band regardless of whether gifsicle ran, was skipped by
                        // the size threshold, or was disabled — keeps the bar monotonic.
                        SetProgressBar(mainForm.pBarTaskStatus, partBase + partSpan, 100);
                        SetStatusText(mainForm, $"Saving part {i + 1} complete");

                        ModifyGifFile(outputPath, canvasHeight);
                    }
                    }
                    finally
                    {
                        gridLayer?.Dispose();
                    }
                }
            });
        }

        #region Slot Machine (拉霸) Methods

        // 766px slot-machine reveal for a STATIC image: each of the 5 Steam columns becomes a vertical
        // reel that wrap-scrolls its own slice, decelerates and locks left-to-right onto the image,
        // then the result is split into the 5 Steam parts.
        public static async Task SlotMachineStaticImage(GifToolMainForm mainForm)
        {
            using var dialog = new SlotMachineDialog(false);
            if (dialog.ShowDialog(mainForm) != DialogResult.OK)
                return;

            ImageInputValidator.ValidateImage(dialog.InputFilePath);
            await RunSlotMachine(mainForm, BuildSettings(dialog, false));
        }

        // Same slot-machine reveal but for an animated GIF: after the reels lock, the GIF plays through.
        public static async Task SlotMachineGif(GifToolMainForm mainForm)
        {
            using var dialog = new SlotMachineDialog(true);
            if (dialog.ShowDialog(mainForm) != DialogResult.OK)
                return;

            ImageInputValidator.ValidateGif(dialog.InputFilePath);
            await RunSlotMachine(mainForm, BuildSettings(dialog, true));
        }

        private static SlotMachineSettings BuildSettings(SlotMachineDialog dialog, bool isGif)
        {
            return new SlotMachineSettings
            {
                InputFilePath = dialog.InputFilePath,
                OutputFilePath = dialog.OutputFilePath,
                IsGif = isGif,
                DurationSeconds = dialog.DurationSeconds,
                DurationVariancePercent = dialog.DurationVariancePercent,
                Fps = dialog.Fps,
                Spins = dialog.Spins,
                SpinsVariancePercent = dialog.SpinsVariancePercent,
                BounceSeconds = dialog.BounceSeconds,
                TopToBottom = dialog.TopToBottom,
                HoldSeconds = dialog.HoldSeconds,
                PlayGifDuringSpin = dialog.PlayGifDuringSpin
            };
        }

        private static async Task RunSlotMachine(GifToolMainForm mainForm, SlotMachineSettings settings)
        {
            // Capture gifsicle settings on the UI thread before any background work.
            var gifsicle = CaptureGifsicleSnapshot(mainForm);
            mainForm.Enabled = false;
            SetProgressRange(mainForm, 0, 100);
            SetProgressBar(mainForm.pBarTaskStatus, 0, 100);
            SetProgressVisible(mainForm, true);

            try
            {
                // Build the full-width 766px slot-machine animation (heavy → background thread).
                // No auto-split: the output is a single 766px GIF so it can be chained with other
                // effects (grid mosaic, scroll, ...) and split with the main "Split GIF" button when
                // ready (that applies the 100px extension + 0x21 tail byte per part).
                await Task.Run(() =>
                {
                    SetStatusText(mainForm, SteamGifCropper.Properties.Resources.Status_SlotMachineBuilding);
                    using var source = new MagickImageCollection(settings.InputFilePath);
                    source.Coalesce();

                    // Auto-resize to 766px wide when the input isn't already a supported width.
                    uint width = source[0].Width;
                    if (!IsValidCanvasWidth(width))
                    {
                        foreach (var frame in source)
                        {
                            frame.ResetPage();
                            frame.Resize(SupportedWidth1, 0);
                        }
                        width = source[0].Width;
                    }

                    var ranges = GetCropRanges(width);
                    int canvasHeight = (int)source[0].Height;

                    using var animation = BuildSlotMachineAnimation(mainForm, source, settings, ranges, (int)width, canvasHeight);
                    SetStatusText(mainForm, SteamGifCropper.Properties.Resources.Status_Saving);
                    animation.Optimize();
                    animation.Write(settings.OutputFilePath);
                });

                // Optional gifsicle on the whole 766px file (size threshold still applies).
                await OptimizeWithGifsicleIfEnabled(mainForm, gifsicle, settings.OutputFilePath);

                SetProgressBar(mainForm.pBarTaskStatus, 100, 100);
                SetStatusText(mainForm, SteamGifCropper.Properties.Resources.Status_Done);
                WindowsThemeManager.ShowThemeAwareMessageBox(mainForm,
                    SteamGifCropper.Properties.Resources.Message_ProcessingComplete,
                    SteamGifCropper.Properties.Resources.Title_Success,
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                SetStatusText(mainForm, SteamGifCropper.Properties.Resources.Status_Error);
                WindowsThemeManager.ShowThemeAwareMessageBox(mainForm,
                    string.Format(SteamGifCropper.Properties.Resources.Error_Occurred, ex.Message),
                    SteamGifCropper.Properties.Resources.Title_Error,
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                mainForm.Enabled = true;
                SetProgressBar(mainForm.pBarTaskStatus, 0, 100);
                SetStatusText(mainForm, SteamGifCropper.Properties.Resources.Status_Idle);
            }
        }

        // Builds the full-width (canvasWidth × canvasHeight) spinning animation. Runs on a background
        // thread; all UI updates go through the marshaled SetProgressBar/SetStatusText helpers.
        private static MagickImageCollection BuildSlotMachineAnimation(GifToolMainForm mainForm,
            MagickImageCollection source, SlotMachineSettings settings,
            (int Start, int End)[] ranges, int canvasWidth, int canvasHeight)
        {
            int reelCount = ranges.Length;
            int spins = Math.Max(1, settings.Spins);

            int srcTicks = (int)source[0].AnimationTicksPerSecond;
            if (srcTicks <= 0) srcTicks = 100;

            // GIF length in seconds (caps spin time and drives the play-during-spin model).
            double gifSeconds = 0.0;
            if (settings.IsGif)
            {
                foreach (var frame in source)
                {
                    gifSeconds += (double)frame.AnimationDelay / srcTicks;
                }
            }
            double maxSpinSeconds = (settings.IsGif && gifSeconds > 0.0) ? gifSeconds : double.MaxValue;

            // Randomize each reel's stop time (seconds) and revolution count so which reel stops first
            // (and which spins longest) is non-deterministic.
            var rng = new Random();
            double[] reelStopSec = new double[reelCount];
            int[] reelSpins = new int[reelCount];
            for (int c = 0; c < reelCount; c++)
            {
                double durSec = SlotMachineGeometry.ApplyVariance(settings.DurationSeconds, settings.DurationVariancePercent, rng.NextDouble());
                if (durSec > maxSpinSeconds) durSec = maxSpinSeconds;
                if (durSec < 0.1) durSec = 0.1;
                reelStopSec[c] = durSec;

                double spinVal = SlotMachineGeometry.ApplyVariance(spins, settings.SpinsVariancePercent, rng.NextDouble());
                reelSpins[c] = Math.Max(1, (int)Math.Round(spinVal));
            }

            if (settings.IsGif && settings.PlayGifDuringSpin)
            {
                return BuildSlotMachinePlayDuringSpin(mainForm, source, settings, ranges, canvasWidth, canvasHeight, reelStopSec, reelSpins, srcTicks, gifSeconds);
            }

            return BuildSlotMachineSpinThenLock(mainForm, source, settings, ranges, canvasWidth, canvasHeight, reelStopSec, reelSpins, srcTicks);
        }

        // Reels spin over a frozen first frame (or the static image) and lock at their randomized times,
        // then either hold the result (static) or play the full GIF (gif "spin, then play").
        private static MagickImageCollection BuildSlotMachineSpinThenLock(GifToolMainForm mainForm,
            MagickImageCollection source, SlotMachineSettings settings,
            (int Start, int End)[] ranges, int canvasWidth, int canvasHeight,
            double[] reelStopSec, int[] reelSpins, int srcTicks)
        {
            int reelCount = ranges.Length;
            int fps = Math.Max(1, settings.Fps);
            int delay = Math.Max(1, (int)Math.Round(100.0 / fps)); // GIF delay in 1/100 s
            int overshootFrames = Math.Max(0, (int)Math.Round(settings.BounceSeconds * fps));

            int[] reelStop = new int[reelCount];
            int maxStop = 1;
            for (int c = 0; c < reelCount; c++)
            {
                reelStop[c] = Math.Max(1, (int)Math.Round(reelStopSec[c] * fps));
                if (reelStop[c] > maxStop) maxStop = reelStop[c];
            }

            int totalSpinFrames = maxStop + overshootFrames;
            int endFrames = settings.IsGif ? source.Count : Math.Max(1, settings.HoldSeconds * fps);
            int totalFrames = totalSpinFrames + endFrames;
            int built = 0;

            var result = new MagickImageCollection();
            var slices = new MagickImage[reelCount];
            try
            {
                // Pre-crop each column slice from the first (locked/"prize") frame.
                for (int c = 0; c < reelCount; c++)
                {
                    int w = ranges[c].End - ranges[c].Start + 1;
                    var slice = (MagickImage)source[0].Clone();
                    slice.Crop(new MagickGeometry(ranges[c].Start, 0, (uint)w, (uint)canvasHeight));
                    slice.ResetPage();
                    slices[c] = slice;
                }

                // Spin phase: each reel wrap-scrolls, decelerates to its own randomized lock, then bounces.
                for (int t = 0; t < totalSpinFrames; t++)
                {
                    var canvas = new MagickImage(MagickColors.Transparent, (uint)canvasWidth, (uint)canvasHeight);
                    for (int c = 0; c < reelCount; c++)
                    {
                        int off = SlotMachineGeometry.ReelOffsetY(t, reelStop[c], reelSpins[c], canvasHeight, settings.TopToBottom, overshootFrames);
                        using var tmp = (MagickImage)slices[c].Clone();
                        if (off != 0)
                        {
                            tmp.Roll(0, off);
                        }
                        canvas.Composite(tmp, ranges[c].Start, 0, CompositeOperator.Over);
                    }
                    canvas.AnimationDelay = (uint)delay;
                    canvas.AnimationTicksPerSecond = 100;
                    canvas.GifDisposeMethod = GifDisposeMethod.Background;
                    result.Add(canvas);

                    if (++built % 5 == 0 || built == totalFrames)
                    {
                        SetProgressBar(mainForm.pBarTaskStatus, built * 100 / totalFrames, 100);
                        SetStatusText(mainForm, SteamGifCropper.Properties.Resources.Status_SlotMachineBuilding);
                    }
                }

                // End phase.
                if (settings.IsGif)
                {
                    // Play the original GIF through once after the reels lock.
                    foreach (var frame in source)
                    {
                        var play = (MagickImage)frame.Clone();
                        play.AnimationTicksPerSecond = srcTicks;
                        play.GifDisposeMethod = GifDisposeMethod.Background;
                        result.Add(play);
                        if (++built % 5 == 0 || built == totalFrames)
                        {
                            SetProgressBar(mainForm.pBarTaskStatus, built * 100 / totalFrames, 100);
                        }
                    }
                }
                else
                {
                    // Hold the locked image for a moment before the loop restarts.
                    for (int h = 0; h < endFrames; h++)
                    {
                        var hold = (MagickImage)source[0].Clone();
                        hold.AnimationDelay = (uint)delay;
                        hold.AnimationTicksPerSecond = 100;
                        hold.GifDisposeMethod = GifDisposeMethod.Background;
                        result.Add(hold);
                        if (++built % 5 == 0 || built == totalFrames)
                        {
                            SetProgressBar(mainForm.pBarTaskStatus, built * 100 / totalFrames, 100);
                        }
                    }
                }
            }
            finally
            {
                foreach (var slice in slices)
                {
                    slice?.Dispose();
                }
            }

            return result;
        }

        // GIF plays on its own timeline; the reels spin over the LIVE frames for the first part, then
        // lock and the GIF keeps playing. Output length == GIF length (the spin consumes its first part).
        private static MagickImageCollection BuildSlotMachinePlayDuringSpin(GifToolMainForm mainForm,
            MagickImageCollection source, SlotMachineSettings settings,
            (int Start, int End)[] ranges, int canvasWidth, int canvasHeight,
            double[] reelStopSec, int[] reelSpins, int srcTicks, double gifSeconds)
        {
            int reelCount = ranges.Length;
            int n = source.Count;

            // Cumulative start time (seconds) of each GIF frame.
            double[] startSec = new double[n];
            double acc = 0.0;
            for (int i = 0; i < n; i++)
            {
                startSec[i] = acc;
                acc += (double)source[i].AnimationDelay / srcTicks;
            }
            double gifFps = (gifSeconds > 0.0) ? n / gifSeconds : n;
            int overshootGif = Math.Max(0, (int)Math.Round(settings.BounceSeconds * gifFps));

            // Convert each reel's stop time (seconds) to a GIF-frame index.
            int[] reelStopFrame = new int[reelCount];
            for (int c = 0; c < reelCount; c++)
            {
                int idx = n;
                for (int i = 0; i < n; i++)
                {
                    if (startSec[i] >= reelStopSec[c]) { idx = i; break; }
                }
                reelStopFrame[c] = Math.Max(1, idx);
            }

            var result = new MagickImageCollection();
            int built = 0;
            for (int i = 0; i < n; i++)
            {
                var canvas = new MagickImage(MagickColors.Transparent, (uint)canvasWidth, (uint)canvasHeight);
                for (int c = 0; c < reelCount; c++)
                {
                    int w = ranges[c].End - ranges[c].Start + 1;
                    using var col = (MagickImage)source[i].Clone();
                    col.Crop(new MagickGeometry(ranges[c].Start, 0, (uint)w, (uint)canvasHeight));
                    col.ResetPage();
                    int off = SlotMachineGeometry.ReelOffsetY(i, reelStopFrame[c], reelSpins[c], canvasHeight, settings.TopToBottom, overshootGif);
                    if (off != 0)
                    {
                        col.Roll(0, off);
                    }
                    canvas.Composite(col, ranges[c].Start, 0, CompositeOperator.Over);
                }
                canvas.AnimationDelay = source[i].AnimationDelay;
                canvas.AnimationTicksPerSecond = srcTicks;
                canvas.GifDisposeMethod = GifDisposeMethod.Background;
                result.Add(canvas);

                if (++built % 5 == 0 || built == n)
                {
                    SetProgressBar(mainForm.pBarTaskStatus, built * 100 / n, 100);
                    SetStatusText(mainForm, SteamGifCropper.Properties.Resources.Status_SlotMachineBuilding);
                }
            }

            return result;
        }

        #endregion

        public static async Task MergeAndSplitFiveGifs(GifToolMainForm mainForm)
        {
            using (var dialog = new MergeFiveGifsDialog())
            {
                if (dialog.ShowDialog() != DialogResult.OK)
                {
                    return; // User cancelled
                }

                await MergeAndSplitFiveGifs(
                    mainForm,
                    dialog.SelectedFilePaths,
                    false,
                    dialog.PaletteSourceIndex);
            }
        }

        public static async Task MergeAndSplitFiveGifs(GifToolMainForm mainForm, List<string> gifFiles, bool useFasterPalette, int paletteSourceIndex)
        {
            if (gifFiles == null || gifFiles.Count != 5)
            {
                return; // Invalid input
            }

            ImageInputValidator.ValidateGifs(gifFiles);

            // Validate all source files exist
            foreach (string gifPath in gifFiles)
            {
                if (!File.Exists(gifPath))
                {
                    WindowsThemeManager.ShowThemeAwareMessageBox(mainForm,
                        string.Format(SteamGifCropper.Properties.Resources.MergeDialog_FileNotFound,
                                      Path.GetFileName(gifPath)),
                        SteamGifCropper.Properties.Resources.Title_Error,
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }

            MagickImageCollection[] collections = null;
            MagickImageCollection[] resizedCollections = null;
            MagickImageCollection[] syncedCollections = null;

            var gifsicle = CaptureGifsicleSnapshot(mainForm);
            mainForm.Enabled = false;
            try
            {
                SetProgressRange(mainForm, 0, 100);
                SetProgressBar(mainForm.pBarTaskStatus, 0, 100);
                SetStatusText(mainForm, SteamGifCropper.Properties.Resources.Status_ValidatingProcessing);

                // Step 2: Load and validate all GIF files (kept on the UI thread: it may pop a
                // color-type error MessageBox which must run on the UI thread).
                collections = LoadAndValidateGifs(gifFiles.ToArray(), mainForm);
                if (collections == null) return;

                SetProgressBar(mainForm.pBarTaskStatus, 20, 100);

                string firstGifPath = gifFiles[0];
                string mergedFileName = $"{Path.GetFileNameWithoutExtension(firstGifPath)}_merged.gif";
                string outputDir = Path.GetDirectoryName(firstGifPath);
                string mergedFilePath = Path.Combine(outputDir, mergedFileName);

                var loadedCollections = collections;
                // Steps 3-5 (resize / synchronize / horizontal merge) are CPU-heavy → background thread.
                await Task.Run(() =>
                {
                    resizedCollections = ResizeGifsToSpecificWidths(loadedCollections, mainForm);
                    SetProgressBar(mainForm.pBarTaskStatus, 40, 100);

                    syncedCollections = SynchronizeToShortestDuration(resizedCollections, mainForm);
                    SetProgressBar(mainForm.pBarTaskStatus, 60, 100);

                    MergeGifsHorizontally(syncedCollections, mergedFilePath, mainForm, useFasterPalette,
                        ResourceLimits.Memory, ResourceLimits.Disk, paletteSourceIndex);
                    SetProgressBar(mainForm.pBarTaskStatus, 80, 100);
                });

                // Step 6: Apply existing split functionality (SplitGif wraps its own Task.Run)
                var ranges = GetCropRanges(SupportedWidth1); // Use 766px ranges
                int adjustedHeight = (int)syncedCollections[0][0].Height + HeightExtension;
                await SplitGif(mergedFilePath, mainForm, ranges, adjustedHeight, gifsicle);

                // Note: mergedFilePath is kept as the intermediate merged file

                SetProgressBar(mainForm.pBarTaskStatus, 100, 100);
                SetStatusText(mainForm, SteamGifCropper.Properties.Resources.MergeFiveGif_Success);
                WindowsThemeManager.ShowThemeAwareMessageBox(mainForm,
                    SteamGifCropper.Properties.Resources.Message_FiveGifMergeComplete,
                    SteamGifCropper.Properties.Resources.Title_Success,
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                SetStatusText(mainForm, SteamGifCropper.Properties.Resources.MergeFiveGif_Error);
                WindowsThemeManager.ShowThemeAwareMessageBox(mainForm,
                    string.Format(SteamGifCropper.Properties.Resources.Error_Processing, ex.Message),
                    SteamGifCropper.Properties.Resources.Title_Error, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                mainForm.Enabled = true;

                // Dispose all collections to prevent memory leaks
                // Note: collections, resizedCollections, and syncedCollections are different objects
                if (collections != null)
                {
                    foreach (var collection in collections)
                    {
                        try { collection?.Dispose(); }
                        catch { /* Log disposal error */ }
                    }
                }

                if (resizedCollections != null)
                {
                    foreach (var collection in resizedCollections)
                    {
                        try { collection?.Dispose(); }
                        catch { /* Log disposal error */ }
                    }
                }

                if (syncedCollections != null)
                {
                    foreach (var collection in syncedCollections)
                    {
                        try { collection?.Dispose(); }
                        catch { /* Log disposal error */ }
                    }
                }
            }
        }


        private static MagickImageCollection[] LoadAndValidateGifs(string[] gifFiles, GifToolMainForm mainForm)
        {
            var collections = new MagickImageCollection[5];

            try
            {
                for (int i = 0; i < 5; i++)
                {
                    collections[i] = new MagickImageCollection(gifFiles[i]);
                    
                    // Validate that GIF has palette colors (8-bit)
                    if (collections[i][0].ColorType != ColorType.Palette && collections[i][0].ColorType != ColorType.PaletteAlpha)
                    {
                        WindowsThemeManager.ShowThemeAwareMessageBox(mainForm,
                            string.Format(SteamGifCropper.Properties.Resources.Error_InvalidColorType,
                                          i + 1, Path.GetFileName(gifFiles[i]), collections[i][0].ColorType),
                            SteamGifCropper.Properties.Resources.Title_InvalidColorType,
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                        
                        // Cleanup loaded collections
                        for (int j = 0; j <= i; j++)
                        {
                            collections[j]?.Dispose();
                        }
                        return null;
                    }
                }
                return collections;
            }
            catch (Exception ex)
            {
                // Cleanup on error
                foreach (var collection in collections)
                {
                    collection?.Dispose();
                }
                throw new InvalidOperationException($"Failed to load and validate GIF files: {ex.Message}", ex);
            }
        }

        private static MagickImageCollection[] ResizeGifsToSpecificWidths(MagickImageCollection[] collections, GifToolMainForm mainForm)
        {
            int[] targetWidths = { 153, 153, 154, 153, 153 };
            var resizedCollections = new MagickImageCollection[5];

            try
            {
                for (int i = 0; i < 5; i++)
                {
                    SetStatusText(mainForm, string.Format(
                        SteamGifCropper.Properties.Resources.Status_ResizingGif,
                        i + 1, targetWidths[i]));
                    resizedCollections[i] = new MagickImageCollection();
                    
                    // Coalesce for proper animation handling
                    collections[i].Coalesce();
                    
                    int frameCount = 0;
                    foreach (var frame in collections[i])
                    {
                        // Resize maintaining aspect ratio
                        frame.Resize((uint)targetWidths[i], 0);
                        resizedCollections[i].Add(frame.Clone());
                        
                        // Update UI every 10 frames to keep responsive
                        if (++frameCount % 10 == 0)
                        {
                            SetStatusText(mainForm, string.Format(
                                SteamGifCropper.Properties.Resources.Status_ResizingGifFrame,
                                i + 1, frameCount, collections[i].Count));                        }
                    }

                    // Copy animation settings
                    for (int j = 0; j < resizedCollections[i].Count; j++)
                    {
                        resizedCollections[i][j].AnimationDelay = collections[i][j].AnimationDelay;
                        
                        // Update UI every 50 frames for animation settings
                        if (j % 50 == 0 && j > 0)
                        {                        }
                    }
                }

                return resizedCollections;
            }
            catch (Exception ex)
            {
                // Cleanup on error
                foreach (var collection in resizedCollections)
                {
                    collection?.Dispose();
                }
                throw new InvalidOperationException($"Failed to resize GIF files: {ex.Message}", ex);
            }
        }

        private static MagickImageCollection[] SynchronizeToShortestDuration(MagickImageCollection[] collections, GifToolMainForm mainForm)
        {
            SetStatusText(mainForm, SteamGifCropper.Properties.Resources.Status_SynchronizingDurations);

            // Calculate total duration for each GIF in seconds
            var durations = new double[5];
            for (int i = 0; i < 5; i++)
            {
                durations[i] = collections[i].Sum(frame => (double)frame.AnimationDelay / frame.AnimationTicksPerSecond);
            }

            // Find shortest duration
            double shortestDuration = durations.Min();
            int shortestIndex = Array.IndexOf(durations, shortestDuration);

            SetStatusText(mainForm, string.Format(
                SteamGifCropper.Properties.Resources.Status_ShortestDuration,
                shortestDuration, shortestIndex + 1));

            // Synchronize all GIFs to shortest duration
            var syncedCollections = new MagickImageCollection[5];

            try
            {
                for (int i = 0; i < 5; i++)
                {
                    SetStatusText(mainForm, string.Format(
                        SteamGifCropper.Properties.Resources.Status_SynchronizingGif,
                        i + 1));
                    
                    syncedCollections[i] = new MagickImageCollection();
                    
                    if (Math.Abs(durations[i] - shortestDuration) < 0.0001)
                    {
                        // Already the shortest, copy as-is
                        int frameCount = 0;
                        foreach (var frame in collections[i])
                        {
                            syncedCollections[i].Add(frame.Clone());
                            
                            // Update every 20 frames
                            if (++frameCount % 20 == 0)
                            {
                                SetStatusText(mainForm, string.Format(
                                    SteamGifCropper.Properties.Resources.Status_SynchronizingGif,
                                    i + 1));
                            }
                        }
                    }
                    else
                    {
                        // Trim to shortest duration
                        double currentDuration = 0;
                        int frameCount = 0;
                        foreach (var frame in collections[i])
                        {
                            double frameDuration = (double)frame.AnimationDelay / frame.AnimationTicksPerSecond;
                            if (currentDuration + frameDuration <= shortestDuration)
                            {
                                syncedCollections[i].Add(frame.Clone());
                                currentDuration += frameDuration;

                                // Update every 20 frames
                                if (++frameCount % 20 == 0)
                                {
                                    SetStatusText(mainForm, string.Format(
                                        SteamGifCropper.Properties.Resources.Status_SynchronizingGif,
                                        i + 1));
                                }
                            }
                            else
                            {
                                break; // Stop when we reach the shortest duration
                            }
                        }
                    }
                    
                    // Set loop animation for each frame
                    foreach (var frame in syncedCollections[i])
                    {
                        frame.GifDisposeMethod = GifDisposeMethod.Background;
                    }
                }

                return syncedCollections;
            }
            catch (Exception ex)
            {
                // Cleanup on error
                foreach (var collection in syncedCollections)
                {
                    collection?.Dispose();
                }
                throw new InvalidOperationException($"Failed to synchronize GIF durations: {ex.Message}", ex);
            }
        }

        private static MagickImage BuildSharedPalette(IEnumerable<MagickImageCollection> collections, bool useFastPalette, int primaryGifIndex = 0)
        {
            var collectionArray = collections.ToArray();
            
            // Use primary GIF's palette as the dominant base
            if (primaryGifIndex >= 0 && primaryGifIndex < collectionArray.Length && 
                collectionArray[primaryGifIndex] != null && collectionArray[primaryGifIndex].Count > 0)
            {
                // Create a palette heavily dominated by the primary GIF
                var paletteSamples = new MagickImageCollection();
                try
                {
                    var primaryGif = collectionArray[primaryGifIndex];
                    
                    // Add primary GIF's first frame 8 times for very strong dominance
                    for (int i = 0; i < 8; i++)
                    {
                        paletteSamples.Add((MagickImage)primaryGif[0].Clone());
                    }
                    
                    // Add other GIFs once each for minimal color blending
                    for (int i = 0; i < collectionArray.Length; i++)
                    {
                        if (i != primaryGifIndex)
                        {
                            var c = collectionArray[i];
                            if (c != null && c.Count > 0)
                            {
                                paletteSamples.Add((MagickImage)c[0].Clone());
                            }
                        }
                    }

                    var settings = new QuantizeSettings
                    {
                        Colors = 256,
                        ColorSpace = ColorSpace.RGB,
                        DitherMethod = useFastPalette ? DitherMethod.No : DitherMethod.FloydSteinberg
                    };

                    if (useFastPalette)
                    {
                        settings.TreeDepth = 5; // Lower tree depth for performance
                    }

                    paletteSamples.Quantize(settings);

                    // Create a copy of the quantized sample to use as palette
                    return new MagickImage(paletteSamples[0]);
                }
                finally
                {
                    paletteSamples.Dispose();
                }
            }
            else
            {
                // Fallback to original equal-weight method if primaryGifIndex is invalid
                var paletteSamples = new MagickImageCollection();
                try
                {
                    foreach (var c in collections)
                    {
                        if (c != null && c.Count > 0)
                        {
                            paletteSamples.Add((MagickImage)c[0].Clone());
                        }
                    }

                    var settings = new QuantizeSettings
                    {
                        Colors = 256,
                        ColorSpace = ColorSpace.RGB,
                        DitherMethod = useFastPalette ? DitherMethod.No : DitherMethod.FloydSteinberg
                    };

                    if (useFastPalette)
                    {
                        settings.TreeDepth = 5; // Lower tree depth for performance
                    }

                    paletteSamples.Quantize(settings);

                    // Create a copy of the quantized sample to use as palette
                    return new MagickImage(paletteSamples[0]);
                }
                finally
                {
                    paletteSamples.Dispose();
                }
            }
        }

        /// <summary>
        /// Merge five GIF collections into a single 766px wide GIF.
        /// </summary>
        /// <param name="collections">Input GIF collections to merge.</param>
        /// <param name="outputPath">Path where the merged GIF will be written.</param>
        /// <param name="mainForm">Main form for updating progress.</param>
        /// <param name="useFastPalette">Whether to use the faster palette generation mode.</param>
        /// <param name="memoryLimitBytes">Maximum memory usage in <c>bytes</c>.</param>
        /// <param name="diskLimitBytes">Maximum temporary disk usage in <c>bytes</c>.</param>
        private static void MergeGifsHorizontally(
            MagickImageCollection[] collections,
            string outputPath,
            GifToolMainForm mainForm,
            bool useFastPalette,
            ulong memoryLimitBytes,
            ulong diskLimitBytes,
            int paletteSourceIndex = 0)
        {
            SetStatusText(mainForm, SteamGifCropper.Properties.Resources.Status_MergingHorizontally);

            // Enable disk caching to limit memory usage
            MagickNET.SetTempDirectory(Path.GetTempPath());

            // Apply resource limits configured by Program.ConfigureResourceLimits
            // Values are in bytes for consistency with that configuration.
            ResourceLimits.Memory = memoryLimitBytes;
            ResourceLimits.Disk = diskLimitBytes;

            // Calculate maximum height among all resized GIFs
            int maxHeight = collections.Max(c => (int)c[0].Height);

            // Build shared palette from first frames
            var palette = BuildSharedPalette(collections, useFastPalette, paletteSourceIndex);

            // Prepare remap settings once
            var mapSettings = new QuantizeSettings
            {
                Colors = 256,
                ColorSpace = ColorSpace.RGB,
                DitherMethod = useFastPalette ? DitherMethod.No : DitherMethod.FloydSteinberg
            };

            int maxFrames = collections.Max(c => c.Count);

            // Configure progress bar for frame-by-frame updates
            SetProgressBar(mainForm.pBarTaskStatus, 0, maxFrames);

            // Create enumerators for each collection to fetch frames on demand
            var enumerators = collections.Select(c => c.GetEnumerator()).ToArray();

            try
            {
                // Accumulate frames into a single collection and write the animation in
                // one pass. Writing frames individually to a shared stream produced N
                // concatenated single-frame GIFs (the gif:write-mode "frame" define is not
                // honored by Magick.NET), so readers only ever saw the first frame.
                // ImageMagick pages pixel data to disk via the temp directory / disk limit
                // configured above, so peak managed memory stays bounded.
                using var output = new MagickImageCollection();

                // X positions for each GIF: 0, 153, 306, 460, 613
                int[] xPositions = { 0, 153, 306, 460, 613 };

                for (int frameIndex = 0; frameIndex < maxFrames; frameIndex++)
                {
                    var canvas = new MagickImage(MagickColors.Transparent, 766, (uint)maxHeight);

                    for (int gifIndex = 0; gifIndex < 5; gifIndex++)
                    {
                        var enumerator = enumerators[gifIndex];
                        if (!enumerator.MoveNext())
                        {
                            enumerator.Dispose();
                            enumerator = collections[gifIndex].GetEnumerator();
                            enumerator.MoveNext();
                            enumerators[gifIndex] = enumerator;
                        }

                        using var frame = (MagickImage)enumerator.Current.Clone();

                        // Composite frame onto canvas at specific X position
                        canvas.Composite(frame, xPositions[gifIndex], 0, CompositeOperator.Over);
                    }

                    // Set animation delay and timing from first GIF to maintain original speed
                    var referenceFrame = (MagickImage)enumerators[0].Current;
                    canvas.AnimationDelay = referenceFrame.AnimationDelay;
                    canvas.AnimationTicksPerSecond = referenceFrame.AnimationTicksPerSecond;

                    canvas.GifDisposeMethod = GifDisposeMethod.Background;

                    // Update status with detailed merging progress
                    if (frameIndex % 10 == 0 || frameIndex == maxFrames - 1)
                    {
                        SetStatusText(mainForm, string.Format("Merging 5 GIFs - Mapping palette for frame {0}/{1}", frameIndex + 1, maxFrames));
                    }

                    // Remap frame to shared palette before writing
                    canvas.Remap(palette, mapSettings);

                    // Collection takes ownership of the canvas; disposed with `output`.
                    output.Add(canvas);
                    UpdateFrameProgressByFrame(mainForm, frameIndex + 1, maxFrames);
                }

                var defines = new GifWriteDefines { RepeatCount = 0 };
                output.Write(outputPath, defines);
            }
            finally
            {
                foreach (var e in enumerators)
                {
                    e.Dispose();
                }
                palette.Dispose();

                // Reset progress bar after merging completes
                SetProgressBar(mainForm.pBarTaskStatus, 0, 100);
            }
        }

        public static void SplitGif(string inputFilePath, string outputDirectory)
        {
            ImageInputValidator.ValidateGif(inputFilePath);
            using var collection = new MagickImageCollection(inputFilePath);
            collection.Coalesce();

            uint canvasWidth = collection[0].Width;
            if (!IsValidCanvasWidth(canvasWidth))
            {
                throw new InvalidOperationException($"Unsupported width: {canvasWidth}");
            }

            var ranges = GetCropRanges(canvasWidth);
            int canvasHeight = (int)collection[0].Height;
            int newHeight = canvasHeight + HeightExtension;
            Directory.CreateDirectory(outputDirectory);

            var (recalculatedDelays, ticksPerSecond) = RecalculateGifDelays(collection);
            collection[0].AnimationTicksPerSecond = ticksPerSecond;

            for (int i = 0; i < ranges.Length; i++)
            {
                using var partCollection = new MagickImageCollection();
                for (int frameIndex = 0; frameIndex < collection.Count; frameIndex++)
                {
                    var frame = collection[frameIndex];
                    int copyWidth = ranges[i].End - ranges[i].Start + 1;

                    using var newImage = new MagickImage(MagickColors.Transparent, (uint)copyWidth, (uint)newHeight);
                    var cropGeometry = new MagickGeometry(ranges[i].Start, 0, (uint)copyWidth, (uint)canvasHeight);
                    using var croppedFrame = frame.Clone();
                    croppedFrame.Crop(cropGeometry);
                    croppedFrame.ResetPage();
                    newImage.Composite(croppedFrame, 0, 0, CompositeOperator.Over);
                    newImage.AnimationDelay = (uint)recalculatedDelays[frameIndex];
                    newImage.AnimationTicksPerSecond = ticksPerSecond;
                    newImage.GifDisposeMethod = GifDisposeMethod.Background;
                    partCollection.Add(newImage.Clone());
                }

                partCollection.Optimize();
                partCollection[0].AnimationTicksPerSecond = ticksPerSecond;
                foreach (var frame in partCollection)
                {
                    frame.Settings.SetDefine("compress", "LZW");
                    frame.Settings.SetDefine(MagickFormat.Gif, "optimize-transparency", "true");
                }

                string outputFile = Path.Combine(outputDirectory, $"{Path.GetFileNameWithoutExtension(inputFilePath)}_Part{i + 1}.gif");
                partCollection.Write(outputFile);
                ModifyGifFile(outputFile, canvasHeight);
            }
        }

        public static async Task SplitGifWithReducedPalette(GifToolMainForm mainForm)
        {
            // Keep the original method name for backward compatibility
            // but redirect to the new merge and split functionality
            await MergeAndSplitFiveGifs(mainForm);
        }

        [Obsolete("This method has been replaced with MergeAndSplitFiveGifs")]
        public static async Task SplitGifWithReducedPaletteOld(GifToolMainForm mainForm)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = SteamGifCropper.Properties.Resources.FileDialog_GifFilter,
                Title = SteamGifCropper.Properties.Resources.FileDialog_SelectGif
            };

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                string inputFilePath = openFileDialog.FileName;
                ImageInputValidator.ValidateGif(inputFilePath);

                try
                {
                    using (var collection = new MagickImageCollection(inputFilePath))
                    {
                        uint canvasWidth = collection[0].Page.Width;
                        uint canvasHeight = collection[0].Page.Height;

                        if (!IsValidCanvasWidth(canvasWidth))
                        {
                            ShowUnsupportedWidthError(canvasWidth);
                            return;
                        }

                        int paletteSize = (int)mainForm.numUpDownPalette.Value; // Get palette size from numericUpDown
                        if (paletteSize < 32 || paletteSize > 256)
                        {
                            WindowsThemeManager.ShowThemeAwareMessageBox(mainForm,
                                            SteamGifCropper.Properties.Resources.Error_PaletteRange,
                                            SteamGifCropper.Properties.Resources.Title_Error,
                                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return;
                        }

                        SetStatusText(mainForm, SteamGifCropper.Properties.Resources.Status_ProcessingPalette);
                        mainForm.pBarTaskStatus.Minimum = 0;
                        mainForm.pBarTaskStatus.Maximum = 100;
                        SetProgressBar(mainForm.pBarTaskStatus, 0, mainForm.pBarTaskStatus.Maximum);

                        var ranges = GetCropRanges(canvasWidth);

                        var progress = new Progress<(int current, int total, string status)>(report =>
                        {
                            mainForm.Invoke((MethodInvoker)(() =>
                            {
                                if (report.total > 0)
                                {
                                    SetProgressBar(mainForm.pBarTaskStatus, Math.Min(report.current * 100 / report.total, 100), mainForm.pBarTaskStatus.Maximum);
                                }
                                SetStatusText(mainForm, report.status);
                            }));
                        });

                        await ReducePaletteAndSplitGif(inputFilePath, ranges, (int)canvasHeight, paletteSize, progress);
                        SetStatusText(mainForm, SteamGifCropper.Properties.Resources.Status_Done);
                        WindowsThemeManager.ShowThemeAwareMessageBox(mainForm,
                                        SteamGifCropper.Properties.Resources.Message_PaletteProcessingComplete,
                                        SteamGifCropper.Properties.Resources.Title_Success, MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
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
                    SetProgressBar(mainForm.pBarTaskStatus, 0, mainForm.pBarTaskStatus.Maximum);
                    SetStatusText(mainForm, SteamGifCropper.Properties.Resources.Status_Idle);
                }
            }
        }

        public static void DiagnoseGif(string gifPath)
        {
            using var collection = new MagickImageCollection(gifPath);
            Console.WriteLine($"Total frames: {collection.Count}");
            Console.WriteLine($"AnimationTicksPerSecond: {collection[0].AnimationTicksPerSecond}");
            Console.WriteLine($"AnimationIterations: {collection[0].AnimationIterations}");
            Console.WriteLine("Frame delays:");
            for (int i = 0; i < Math.Min(collection.Count, 10); i++)
            {
                Console.WriteLine($"Frame {i}: {collection[i].AnimationDelay} cs");
            }
            double totalDelay = collection.Sum(frame => (double)frame.AnimationDelay);
            double averageFps = collection.Count * 100.0 / totalDelay;
            Console.WriteLine($"Average FPS: {averageFps:F2}");
        }

        private static async Task ReducePaletteAndSplitGif(string inputFilePath, (int Start, int End)[] ranges, int canvasHeight, int paletteSize, IProgress<(int current, int total, string status)> progress)
        {
            await Task.Run(() =>
            {
                using (var collection = new MagickImageCollection(inputFilePath))
                {
                    progress?.Report((0, 1, SteamGifCropper.Properties.Resources.Status_CoalescingFrames));
                    collection.Coalesce();

                    int newHeight = canvasHeight + HeightExtension;

                    // Preserve original frame delays
                    var originalDelays = collection.Select(f => (int)f.AnimationDelay).ToArray();

                    int totalSteps = (collection.Count * ranges.Length) + (ranges.Length * 3); // Processing + Palette reduction + LZW compression
                    int currentStep = 0;

                    for (int i = 0; i < ranges.Length; i++)
                    {
                        using (var partCollection = new MagickImageCollection())
                        {
                            for (int frameIndex = 0; frameIndex < collection.Count; frameIndex++)
                            {
                                var frame = collection[frameIndex];
                                int copyWidth = ranges[i].End - ranges[i].Start + 1;

                                // Create new image with correct dimensions
                                using (var newImage = new MagickImage(MagickColors.Transparent, (uint)copyWidth, (uint)newHeight))
                                {
                                    // Crop the frame to the specific range
                                    var cropGeometry = new MagickGeometry(ranges[i].Start, 0, (uint)copyWidth, (uint)canvasHeight);
                                    using (var croppedFrame = frame.Clone())
                                    {
                                        croppedFrame.Crop(cropGeometry);
                                        croppedFrame.ResetPage();

                                        // Composite the cropped frame onto the new image
                                        newImage.Composite(croppedFrame, 0, 0, CompositeOperator.Over);
                                    }

                                    // Set animation properties to preserve original timing
                                    newImage.AnimationDelay = (uint)originalDelays[frameIndex];
                                    newImage.GifDisposeMethod = GifDisposeMethod.Background;

                                    // Apply palette reduction
                                    newImage.Quantize(new QuantizeSettings { Colors = (uint)paletteSize });

                                    partCollection.Add(newImage.Clone());
                                }

                                currentStep++;
                                progress?.Report((currentStep, totalSteps, string.Format(SteamGifCropper.Properties.Resources.Status_ProcessingPartPalette, i + 1, currentStep % collection.Count + 1)));
                            }

                            string outputFile = $"{Path.GetFileNameWithoutExtension(inputFilePath)}_Part{i + 1}_Palette{paletteSize}.gif";
                            string outputDir = Path.GetDirectoryName(inputFilePath);
                            string outputPath = Path.Combine(outputDir, outputFile);

                            partCollection.Optimize();
                            partCollection[0].AnimationTicksPerSecond = 100;
                            currentStep++;
                            progress?.Report((currentStep, totalSteps, SteamGifCropper.Properties.Resources.Status_Compressing));

                            foreach (var frame in partCollection)
                            {
                                frame.Settings.SetDefine("compress", "LZW");
                                frame.Settings.SetDefine(MagickFormat.Gif, "optimize-transparency", "true");
                            }

                            currentStep++;
                            progress?.Report((currentStep, totalSteps, SteamGifCropper.Properties.Resources.Status_Saving));

                            partCollection.Write(outputPath);

                            currentStep++;
                            progress?.Report((currentStep, totalSteps, SteamGifCropper.Properties.Resources.Status_Saving));

                            ModifyGifFile(outputPath, canvasHeight);
                        }
                    }
                }
            });
        }
        private static void ModifyGifFile(string filePath, int adjustedHeight)
        {
            try
            {
                byte[] fileData = File.ReadAllBytes(filePath);
                if (fileData.Length < 10)
                {
                    throw new InvalidOperationException($"Invalid GIF file: {filePath}");
                }

                // Modify tail byte from 0x3B to 0x21
                fileData[fileData.Length - 1] = 0x21;

                // Update height bytes
                ushort heightValue = (ushort)adjustedHeight;
                fileData[8] = (byte)(heightValue & 0xFF);
                fileData[9] = (byte)((heightValue >> 8) & 0xFF);

                File.WriteAllBytes(filePath, fileData);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to modify GIF file {filePath}: {ex.Message}", ex);
            }
        }
        public static void ResizeGifTo766(string inputFilePath, string outputFilePath, GifToolMainForm mainForm = null)
        {
            ImageInputValidator.ValidateGif(inputFilePath);
            try
            {
                SetStatusText(mainForm, SteamGifCropper.Properties.Resources.Status_CoalescingFrames);
                using (var collection = new MagickImageCollection(inputFilePath))
                {
                    collection.Coalesce();

                    int totalFrames = collection.Count;
                    int currentFrame = 0;

                    if (mainForm != null)
                    {
                        SetProgressRange(mainForm, 0, totalFrames);
                        SetProgressBar(mainForm.pBarTaskStatus, 0, totalFrames);
                        UpdateFrameProgressByFrame(mainForm, 0, totalFrames);
                    }

                    foreach (var frame in collection)
                    {
                        frame.ResetPage();
                        frame.Resize(SupportedWidth1, 0);
                        frame.Settings.SetDefine("compress", "LZW");

                        currentFrame++;
                        if (mainForm != null)
                        {
                            UpdateFrameProgressByFrame(mainForm, currentFrame, totalFrames);
                        }
                    }

                    // These two steps are a single long blocking call each with no sub-progress; after
                    // "N/N" the bar would otherwise sit still, so surface the phase in the status text.
                    SetStatusText(mainForm, SteamGifCropper.Properties.Resources.Status_Optimizing);
                    collection.Optimize();

                    SetStatusText(mainForm, SteamGifCropper.Properties.Resources.Status_Saving);
                    collection.Write(outputFilePath);
                }
            }
            finally
            {
                if (mainForm != null)
                {
                    SetProgressBar(mainForm.pBarTaskStatus, 0, 100);
                    SetStatusText(mainForm, SteamGifCropper.Properties.Resources.Status_Idle);
                }
            }
        }

        public static async Task ResizeGifTo766(GifToolMainForm mainForm)
        {
            using (var openFileDialog = new OpenFileDialog
            {
                Filter = SteamGifCropper.Properties.Resources.FileDialog_GifFilter,
                Title = SteamGifCropper.Properties.Resources.FileDialog_SelectGifResize
            })
            {
                if (openFileDialog.ShowDialog() != DialogResult.OK) return;

                string inputFilePath = openFileDialog.FileName;
                ImageInputValidator.ValidateGif(inputFilePath);
                string outputFilePath = GenerateOutputPath(inputFilePath, "_766px");

                mainForm.Enabled = false;
                try
                {
                    SetProgressRange(mainForm, 0, 100);
                    SetProgressBar(mainForm.pBarTaskStatus, 0, 100);
                    SetProgressVisible(mainForm, true);
                    SetStatusText(mainForm, SteamGifCropper.Properties.Resources.Status_Loading);

                    await Task.Run(() => ResizeGifTo766(inputFilePath, outputFilePath, mainForm));

                    WindowsThemeManager.ShowThemeAwareMessageBox(mainForm,
                                    string.Format(SteamGifCropper.Properties.Resources.Message_ResizeComplete,
                                                  Path.GetFileName(outputFilePath)),
                                    SteamGifCropper.Properties.Resources.Title_Success, MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    WindowsThemeManager.ShowThemeAwareMessageBox(mainForm,
                                    string.Format(SteamGifCropper.Properties.Resources.Error_ResizeFailed, ex.Message),
                                    SteamGifCropper.Properties.Resources.Title_Error, MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                finally
                {
                    mainForm.Enabled = true;
                    SetProgressBar(mainForm.pBarTaskStatus, 0, 100);
                    SetStatusText(mainForm, SteamGifCropper.Properties.Resources.Status_Idle);
                }
            }
        }

        private static string GenerateOutputPath(string inputPath, string suffix)
        {
            string directory = Path.GetDirectoryName(inputPath);
            string fileName = Path.GetFileNameWithoutExtension(inputPath);
            string extension = Path.GetExtension(inputPath);
            return Path.Combine(directory, $"{fileName}{suffix}{extension}");
        }
        public static void RestoreTailByteForMultipleGifs(GifToolMainForm mainForm)
        {
            using (var openFileDialog = new OpenFileDialog
            {
                Filter = SteamGifCropper.Properties.Resources.FileDialog_GifFilter,
                Title = SteamGifCropper.Properties.Resources.FileDialog_SelectGifRestore,
                Multiselect = true
            })
            {
                if (openFileDialog.ShowDialog() != DialogResult.OK) return;

                string[] selectedFiles = openFileDialog.FileNames;
                ImageInputValidator.ValidateGifs(selectedFiles);
                int processedCount = 0;
                int skippedCount = 0;

                mainForm.Enabled = false;
                try
                {
                    SetStatusText(mainForm, SteamGifCropper.Properties.Resources.Status_RestoringTailBytes);
                    SetProgressBar(mainForm.pBarTaskStatus, 0, mainForm.pBarTaskStatus.Maximum);
                    mainForm.pBarTaskStatus.Maximum = 100;
                    mainForm.pBarTaskStatus.Visible = true;

                    int progress = 0;
                    foreach (string filePath in selectedFiles)
                    {
                        try
                        {
                            if (RestoreGifTailByte(filePath))
                            {
                                processedCount++;
                            }
                            else
                            {
                                skippedCount++;
                            }
                        }
                        catch (Exception ex)
                        {
                            WindowsThemeManager.ShowThemeAwareMessageBox(mainForm,
                                string.Format(SteamGifCropper.Properties.Resources.Error_ProcessingFile,
                                              Path.GetFileName(filePath), ex.Message),
                                SteamGifCropper.Properties.Resources.Title_FileProcessingError,
                                MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            skippedCount++;
                        }

                        progress++;
                        SetProgressBar(mainForm.pBarTaskStatus, progress, selectedFiles.Length);
                        if (progress % ProgressUpdateInterval == 0 || progress == selectedFiles.Length)
                        {
                            SetStatusText(mainForm, string.Format(
                                "Restoring tail bytes {0}/{1}: {2}",
                                progress, selectedFiles.Length, Path.GetFileName(filePath)));
                        }
                    }

                    string resultMessage = string.Format(
                        SteamGifCropper.Properties.Resources.Message_RestorationComplete,
                        processedCount, skippedCount)
                        .Replace("\n", Environment.NewLine);

                    WindowsThemeManager.ShowThemeAwareMessageBox(mainForm,
                                    resultMessage,
                                    SteamGifCropper.Properties.Resources.Title_TailByteRestoration,
                                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    WindowsThemeManager.ShowThemeAwareMessageBox(mainForm,
                                  string.Format(SteamGifCropper.Properties.Resources.Error_Occurred, ex.Message),
                                  SteamGifCropper.Properties.Resources.Title_Error, MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                finally
                {
                    mainForm.Enabled = true;
                    SetProgressBar(mainForm.pBarTaskStatus, 0, mainForm.pBarTaskStatus.Maximum);
                    //mainForm.pBarTaskStatus.Visible = false;
                    SetStatusText(mainForm, SteamGifCropper.Properties.Resources.Status_Ready);
                }
            }
        }

        private static bool RestoreGifTailByte(string filePath)
        {
            try
            {
                byte[] fileData = File.ReadAllBytes(filePath);
                
                if (fileData.Length == 0)
                {
                    return false;
                }

                // Check if the last byte is 0x21
                if (fileData[fileData.Length - 1] != 0x21)
                {
                    // File doesn't have 0x21 as last byte, skip it
                    return false;
                }

                // Change 0x21 to 0x3B
                fileData[fileData.Length - 1] = 0x3B;
                
                File.WriteAllBytes(filePath, fileData);
                return true;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to restore tail byte for {filePath}: {ex.Message}", ex);
            }
        }

        public static void WriteTailByteForMultipleGifs(GifToolMainForm mainForm)
        {
            using (var openFileDialog = new OpenFileDialog
            {
                Filter = SteamGifCropper.Properties.Resources.FileDialog_GifFilter,
                Title = SteamGifCropper.Properties.Resources.FileDialog_SelectGifFiles,
                Multiselect = true
            })
            {
                if (openFileDialog.ShowDialog() != DialogResult.OK) return;

                string[] filePaths = openFileDialog.FileNames;
                ImageInputValidator.ValidateGifs(filePaths);
                int processedFiles = 0;

                mainForm.Enabled = false;
                try
                {
                    mainForm.pBarTaskStatus.Visible = true;
                    SetProgressBar(mainForm.pBarTaskStatus, 0, mainForm.pBarTaskStatus.Maximum);
                    mainForm.pBarTaskStatus.Maximum = 100;

                    foreach (string filePath in filePaths)
                    {
                        try
                        {
                            // Update status with current file being processed
                            SetStatusText(mainForm, string.Format(
                                "Modifying tail bytes {0}/{1}: {2}",
                                processedFiles + 1,
                                filePaths.Count(),
                                Path.GetFileName(filePath)));
                                
                            if (ProcessTailByte(filePath))
                                processedFiles++;

                            SetProgressBar(mainForm.pBarTaskStatus, processedFiles, filePaths.Length);
                        }
                        catch (Exception ex)
                        {
                            WindowsThemeManager.ShowThemeAwareMessageBox(
                                mainForm,
                                string.Format(SteamGifCropper.Properties.Resources.Error_ProcessingFile,
                                              Path.GetFileName(filePath), ex.Message),
                                SteamGifCropper.Properties.Resources.Title_FileProcessingError,
                                MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }
                    }

                    WindowsThemeManager.ShowThemeAwareMessageBox(
                        mainForm,
                        string.Format(SteamGifCropper.Properties.Resources.Message_ProcessedFiles,
                                      processedFiles, filePaths.Length),
                        SteamGifCropper.Properties.Resources.Title_Success,
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    WindowsThemeManager.ShowThemeAwareMessageBox(mainForm, string.Format(SteamGifCropper.Properties.Resources.Error_Occurred, ex.Message),
                                    SteamGifCropper.Properties.Resources.Title_Error, MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                finally
                {
                    mainForm.Enabled = true;
                    SetProgressBar(mainForm.pBarTaskStatus, 0, mainForm.pBarTaskStatus.Maximum);
                    //mainForm.pBarTaskStatus.Visible = false;
                    SetStatusText(mainForm, SteamGifCropper.Properties.Resources.Status_Idle);
                }
            }
        }

        private static bool ProcessTailByte(string filePath)
        {
            const byte gifTrailer = 0x3B;
            const byte modifiedTrailer = 0x21;

            byte[] fileData = File.ReadAllBytes(filePath);
            if (fileData.Length == 0) return false;

            if (fileData[fileData.Length - 1] == gifTrailer)
            {
                fileData[fileData.Length - 1] = modifiedTrailer;
                File.WriteAllBytes(filePath, fileData);
                return true;
            }
            return false;
        }

        public static async Task MergeMultipleGifs(List<string> gifPaths, string outputPath, GifToolMainForm mainForm, bool useFastPalette = false, int primaryGifIndex = 0)
        {
            if (gifPaths == null || gifPaths.Count < 2 || gifPaths.Count > 5)
            {
                throw new ArgumentException(SteamGifCropper.Properties.Resources.Message_GifFileCount);
            }

            // Validate source files and destination path
            SetStatusText(mainForm, "Validate file paths...");
            ImageInputValidator.ValidateGifs(gifPaths);
            foreach (string gifPath in gifPaths)
            {
                if (!File.Exists(gifPath))
                {
                    throw new FileNotFoundException($"Source file not found: {Path.GetFileName(gifPath)}");
                }
            }
            
            var outputDir = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
            {
                try
                {
                    Directory.CreateDirectory(outputDir);
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Cannot create output directory: {ex.Message}");
                }
            }

            var collections = new List<MagickImageCollection>();
            try
            {
                mainForm.Enabled = false;
                SetStatusText(mainForm, SteamGifCropper.Properties.Resources.Message_AnalyzingGifs);
                var widths = new List<int>();
                int minFrameCount = int.MaxValue;

                // Load all GIFs (heavy: decode + coalesce) on a background thread.
                await Task.Run(() =>
                {
                    foreach (string gifPath in gifPaths)
                    {
                        SetStatusText(mainForm, gifPath);
                        var collection = new MagickImageCollection(gifPath);
                        collection.Coalesce();
                        collections.Add(collection);

                        widths.Add((int)collection[0].Width);
                        minFrameCount = Math.Min(minFrameCount, collection.Count);
                    }
                });

                // Check for FPS mismatches and warn user
                var fpsValues = new List<double>();
                foreach (var collection in collections)
                {
                    var firstFrame = collection[0];
                    double fps = firstFrame.AnimationDelay > 0 ? 
                        (double)firstFrame.AnimationTicksPerSecond / firstFrame.AnimationDelay : 15.0;
                    fpsValues.Add(fps);
                }

                // Check if all FPS values are significantly different (tolerance of 0.5 FPS)
                var distinctFps = fpsValues.Where(fps => fpsValues.Any(other => Math.Abs(fps - other) > 0.5)).Distinct().ToList();
                if (distinctFps.Count > 1)
                {
                    WindowsThemeManager.ShowThemeAwareMessageBox(mainForm,
                        SteamGifCropper.Properties.Resources.Warning_FPS_Mismatch,
                        "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }

                // Use first GIF's timing settings for the merged result
                var referenceFrame = collections[0][0];
                int ticksPerSecond = referenceFrame.AnimationTicksPerSecond;

                // Use the minimum frame count to avoid extending shorter animations
                int targetFrameCount = minFrameCount;

                // Calculate total width
                int totalWidth = widths.Sum();
                int maxHeight = collections.Max(c => (int)c[0].Height);

                SetStatusText(mainForm, SteamGifCropper.Properties.Resources.Message_MergingGifs);

                var mergedCollection = new MagickImageCollection();

                try
                {
                    // All compositing / palette remap / LZW / write is CPU-heavy → background thread.
                    await Task.Run(() =>
                    {
                        // Build shared palette from first frames
                        var palette = BuildSharedPalette(collections, useFastPalette, primaryGifIndex);
                        try
                        {
                            for (int frameIndex = 0; frameIndex < targetFrameCount; frameIndex++)
                            {
                                // Update progress more frequently for better user feedback
                                if (frameIndex % 2 == 0 || frameIndex == targetFrameCount - 1)
                                {
                                    SetStatusText(mainForm, $"{SteamGifCropper.Properties.Resources.Message_MergingGifs} ({frameIndex + 1}/{targetFrameCount})");
                                }

                                // Create canvas with total width
                                var canvas = new MagickImage(MagickColors.Transparent, (uint)totalWidth, (uint)maxHeight);

                                int currentX = 0;

                                for (int gifIndex = 0; gifIndex < collections.Count; gifIndex++)
                                {
                                    var collection = collections[gifIndex];

                                    // Calculate which frame to use based on shortest duration
                                    double frameProgress = (double)frameIndex / targetFrameCount;
                                    int sourceFrameIndex = Math.Min((int)(frameProgress * collection.Count), collection.Count - 1);

                                    var frame = collection[sourceFrameIndex];

                                    // Composite frame onto canvas at current X position
                                    canvas.Composite(frame, currentX, 0, CompositeOperator.Over);

                                    currentX += widths[gifIndex];
                                }

                                // Set animation delay and timing from the reference frame
                                var sourceFrame = collections[0][Math.Min(frameIndex, collections[0].Count - 1)];
                                canvas.AnimationDelay = sourceFrame.AnimationDelay;
                                canvas.AnimationTicksPerSecond = ticksPerSecond;
                                canvas.GifDisposeMethod = GifDisposeMethod.Background;

                                mergedCollection.Add(canvas);
                            }

                            // Remap frames to shared palette
                            SetStatusText(mainForm, useFastPalette ?
                                SteamGifCropper.Properties.Resources.Status_MappingFastPalette :
                                SteamGifCropper.Properties.Resources.Status_MappingSharedPalette);
                            var mapSettings = new QuantizeSettings
                            {
                                Colors = 256,
                                ColorSpace = ColorSpace.RGB,
                                DitherMethod = useFastPalette ? DitherMethod.No : DitherMethod.FloydSteinberg
                            };

                            int totalFrames = mergedCollection.Count;
                            int currentFrame = 0;
                            foreach (MagickImage frame in mergedCollection)
                            {
                                currentFrame++;
                                frame.Remap(palette, mapSettings);

                                // Update progress every frame or every 5 frames for better responsiveness
                                if (currentFrame % Math.Max(1, totalFrames / 20) == 0 || currentFrame == totalFrames)
                                {
                                    int progress = (int)((double)currentFrame / totalFrames * 100);
                                    SetProgressBar(mainForm.pBarTaskStatus, progress, 100);
                                    SetStatusText(mainForm, string.Format(
                                        useFastPalette ? "Fast palette mapping: {0}/{1} ({2}%)" : "Quality palette mapping: {0}/{1} ({2}%)",
                                        currentFrame, totalFrames, progress));
                                }
                            }

                            // Apply LZW compression
                            SetStatusText(mainForm, "Processing LZW compression...");
                            foreach (var frame in mergedCollection)
                            {
                                frame.Format = MagickFormat.Gif;
                                frame.Settings.SetDefine(MagickFormat.Gif, "optimize-transparency", "true");
                            }

                            // Save the merged GIF
                            SetStatusText(mainForm, SteamGifCropper.Properties.Resources.Status_Saving);
                            mergedCollection.Write(outputPath);
                        }
                        finally
                        {
                            palette.Dispose();
                        }
                    });

                    string successMessage = string.Format(SteamGifCropper.Properties.Resources.Message_GifMergeComplete, outputPath);
                    WindowsThemeManager.ShowThemeAwareMessageBox(mainForm, successMessage, SteamGifCropper.Properties.Resources.Title_Success, MessageBoxButtons.OK, MessageBoxIcon.Information);

                    SetStatusText(mainForm, SteamGifCropper.Properties.Resources.Status_Done);
                }
                finally
                {
                    mergedCollection?.Dispose();
                }
            }
            catch (Exception ex)
            {
                string errorMessage = $"Error merging GIF files: {ex.Message}";
                WindowsThemeManager.ShowThemeAwareMessageBox(mainForm, errorMessage, SteamGifCropper.Properties.Resources.Title_MergeGifError, MessageBoxButtons.OK, MessageBoxIcon.Error);
                SetStatusText(mainForm, SteamGifCropper.Properties.Resources.Status_Error);
                throw;
            }
            finally
            {
                mainForm.Enabled = true;
                // Clean up collections
                if (collections != null)
                {
                    foreach (var collection in collections)
                    {
                        collection?.Dispose();
                    }
                }
            }
        }

        public static async Task ConvertMp4ToGif(GifToolMainForm mainForm)
        {
            // Check if FFmpeg is available with detailed diagnostics
            var (isAvailable, ffmpegPath, ffmpegVersion, error) = GetFFmpegDiagnostics();
            
            if (!isAvailable)
            {
                string errorPart = string.IsNullOrEmpty(error) ? string.Empty : $"Error: {error}\n";
                string diagMessage = string.Format(SteamGifCropper.Properties.Resources.Mp4ToGif_FFmpegRequiredMessage,
                                                   ffmpegPath ?? "Not found",
                                                   ffmpegVersion ?? "N/A",
                                                   errorPart);
                WindowsThemeManager.ShowThemeAwareMessageBox(mainForm,
                                diagMessage,
                                SteamGifCropper.Properties.Resources.Title_FFmpegRequired,
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Warning);
                return;
            }

            // Show MP4 to GIF conversion dialog
            using (var conversionDialog = new Mp4ToGifDialog())
            {
                if (conversionDialog.ShowDialog() != DialogResult.OK)
                    return;

                // Get conversion parameters
                var inputPath = conversionDialog.InputFilePath;
                var outputPath = conversionDialog.OutputFilePath;
                var startTime = conversionDialog.StartTime;
                var duration = conversionDialog.Duration;
                var targetFramerate = (int)mainForm.numUpDownFramerate.Value;

                mainForm.Enabled = false;
                try
                {
                    mainForm.pBarTaskStatus.Visible = true;
                    SetProgressBar(mainForm.pBarTaskStatus, 0, mainForm.pBarTaskStatus.Maximum);
                    
                    SetStatusText(mainForm, "Analyzing video...");
                    SetProgressBar(mainForm.pBarTaskStatus, 10, mainForm.pBarTaskStatus.Maximum);
                    await Task.Delay(100);
                    
                    SetStatusText(mainForm, "Generating optimal color palette...");
                    SetProgressBar(mainForm.pBarTaskStatus, 30, mainForm.pBarTaskStatus.Maximum);
                    await Task.Delay(100);
                    
                    SetStatusText(mainForm, "Converting video to GIF...");
                    SetProgressBar(mainForm.pBarTaskStatus, 50, mainForm.pBarTaskStatus.Maximum);
                    await Task.Delay(100);
                    
                    await ProcessWithOptimizedCpu(inputPath, outputPath, startTime, duration, targetFramerate);

                    SetProgressBar(mainForm.pBarTaskStatus, 100, mainForm.pBarTaskStatus.Maximum);
                    SetStatusText(mainForm, SteamGifCropper.Properties.Resources.Mp4ToGif_Success);
                    
                    WindowsThemeManager.ShowThemeAwareMessageBox(mainForm,
                                  string.Format(SteamGifCropper.Properties.Resources.Mp4ToGif_SuccessMessage, Path.GetFileName(outputPath)),
                                  SteamGifCropper.Properties.Resources.Title_Success, MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    string detailedError = ex.ToString();
                    string userFriendlyMessage;

                    // Capture detailed FFmpeg output if available
                    string ffmpegOutput = null;
                    string logFilePath = null;
                    if (ex is FFMpegException ffmpegException && !string.IsNullOrWhiteSpace(ffmpegException.FFMpegErrorOutput))
                    {
                        ffmpegOutput = ffmpegException.FFMpegErrorOutput;
                        try
                        {
                            string logDirectory = Path.GetDirectoryName(outputPath);
                            if (string.IsNullOrEmpty(logDirectory) || !Directory.Exists(logDirectory))
                                logDirectory = Path.GetTempPath();

                            logFilePath = Path.Combine(logDirectory, "ffmpeg_error.log");
                            File.WriteAllText(logFilePath, ffmpegOutput);
                        }
                        catch
                        {
                            // Ignore logging failures
                        }
                    }

                    if (ex.Message.Contains("No such file or directory") || ex.Message.Contains("not found"))
                    {
                        userFriendlyMessage = SteamGifCropper.Properties.Resources.Mp4ToGif_Error_FFmpegNotFound;
                    }
                    else if (ex.Message.Contains("Invalid data found") || ex.Message.Contains("moov atom not found"))
                    {
                        userFriendlyMessage = SteamGifCropper.Properties.Resources.Mp4ToGif_Error_CorruptedInput;
                    }
                    else if (ex.Message.Contains("Permission denied") || ex.Message.Contains("Access is denied"))
                    {
                        userFriendlyMessage = SteamGifCropper.Properties.Resources.Mp4ToGif_Error_PermissionDenied;
                    }
                    else
                    {
                        userFriendlyMessage = SteamGifCropper.Properties.Resources.Mp4ToGif_Error_Unexpected;
                    }

                    // Append FFmpeg stderr details
                    if (!string.IsNullOrEmpty(ffmpegOutput))
                    {
                        if (!string.IsNullOrEmpty(logFilePath))
                        {
                            userFriendlyMessage += string.Format(SteamGifCropper.Properties.Resources.Mp4ToGif_Error_DetailsSaved, logFilePath);
                        }
                        else
                        {
                            string truncated = ffmpegOutput.Length > 500 ? ffmpegOutput.Substring(0, 500) + "..." : ffmpegOutput;
                            userFriendlyMessage += string.Format(SteamGifCropper.Properties.Resources.Mp4ToGif_Error_FFmpegOutputTruncated, truncated);
                        }
                    }

                    WindowsThemeManager.ShowThemeAwareMessageBox(mainForm,
                                                  string.Format(SteamGifCropper.Properties.Resources.Mp4ToGif_ErrorMessageDetails,
                                                  userFriendlyMessage, inputPath, outputPath, startTime, duration, ex.Message),
                                  SteamGifCropper.Properties.Resources.Title_Mp4ToGifError, MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                finally
                {
                    mainForm.Enabled = true;
                    SetProgressBar(mainForm.pBarTaskStatus, 0, mainForm.pBarTaskStatus.Maximum);
                    //mainForm.pBarTaskStatus.Visible = false;
                    SetStatusText(mainForm, SteamGifCropper.Properties.Resources.Status_Ready);
                }
            }
        }

        private static bool IsFFmpegAvailable()
        {
            var (isAvailable, _, _, _) = GetFFmpegDiagnostics();
            return isAvailable;
        }

        private static async Task ProcessWithOptimizedCpu(string inputPath, string outputPath, TimeSpan startTime, TimeSpan duration, int targetFramerate = 25)
        {
            // Use direct file input/output instead of pipes for better compatibility
            try
            {
                var token = CreateFfmpegCancellationToken();
                await FFMpegArguments
                    .FromFileInput(inputPath)
                    .OutputToFile(outputPath, true, options =>
                    {
                        options.ForceFormat("gif");

                        // Only apply seek if startTime is greater than 0
                        if (startTime > TimeSpan.Zero)
                        {
                            options.Seek(startTime);
                        }

                        // Only apply duration if it's reasonable (not too short)
                        if (duration > TimeSpan.FromSeconds(0.1))
                        {
                            options.WithDuration(duration);
                        }

                        options.WithFramerate(targetFramerate)
                               .WithCustomArgument("-pix_fmt rgb8")
                               .WithCustomArgument("-an");
                        ApplyThreadLimit(options);
                    })
                    .CancellableThrough(token)
                    .ProcessAsynchronously();
            }
            catch (FFMpegException ex) when (ex.FFMpegErrorOutput?.Contains("partial file") == true ||
                                           ex.FFMpegErrorOutput?.Contains("Invalid argument") == true ||
                                           ex.FFMpegErrorOutput?.Contains("Error during demuxing") == true)
            {
                // Retry without seek and duration parameters if file reading fails
                // This handles cases where the video file format or codec has issues with seeking
                // Create a new CancellationToken for the retry attempt
                var retryToken = CreateFfmpegCancellationToken();
                await FFMpegArguments
                    .FromFileInput(inputPath)
                    .OutputToFile(outputPath, true, options =>
                    {
                        options.ForceFormat("gif")
                               .WithFramerate(targetFramerate)
                               .WithCustomArgument("-pix_fmt rgb8")
                               .WithCustomArgument("-an")
                               .WithCustomArgument("-avoid_negative_ts make_zero"); // Handle timing issues
                        ApplyThreadLimit(options);
                    })
                    .CancellableThrough(retryToken)
                    .ProcessAsynchronously();
            }
        }

        private static (bool isAvailable, string ffmpegPath, string version, string error) GetFFmpegDiagnostics()
        {
            try
            {
                // First, try to find FFmpeg in PATH
                string ffmpegPath = "ffmpeg"; // Will use PATH lookup

                using var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = ffmpegPath,
                        Arguments = "-version",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();

                bool finished = process.WaitForExit(5000); // Wait max 5 seconds

                if (!finished)
                {
                    try
                    {
                        process.Kill();
                    }
                    catch (Exception ex)
                    {
                        // Log process kill failure but don't throw
                        System.Diagnostics.Debug.WriteLine($"Failed to kill FFmpeg process: {ex.Message}");
                    }
                    return (false, ffmpegPath, null, "FFmpeg process timed out");
                }

                if (process.ExitCode == 0 && (output.Contains("ffmpeg version") || output.Contains("configuration:")))
                {
                    // Extract version from output
                    string version = "Unknown";
                    var lines = output.Split('\n');
                    if (lines.Length > 0 && lines[0].Contains("ffmpeg version"))
                    {
                        version = lines[0].Trim();
                    }

                    return (true, ffmpegPath, version, null);
                }
                else
                {
                    return (false, ffmpegPath, null, $"Exit code: {process.ExitCode}, Output: {output}, Error: {error}");
                }
            }
            catch (Exception ex)
            {
                return (false, null, null, ex.Message);
            }
        }

        public static async Task ReverseGif(GifToolMainForm mainForm)
        {
            using (var openFileDialog = new OpenFileDialog
            {
                Filter = SteamGifCropper.Properties.Resources.FileDialog_GifFilter,
                Title = SteamGifCropper.Properties.Resources.FileDialog_SelectGifToReverse ?? "Select a GIF file to reverse"
            })
            {
                if (openFileDialog.ShowDialog() != DialogResult.OK) return;

                string inputFilePath = openFileDialog.FileName;
                ImageInputValidator.ValidateGif(inputFilePath);
                string inputFileName = Path.GetFileNameWithoutExtension(inputFilePath);
                string inputDirectory = Path.GetDirectoryName(inputFilePath);
                string outputFilePath = Path.Combine(inputDirectory, $"{inputFileName}_reversed.gif");

                using (var saveFileDialog = new SaveFileDialog
                {
                    Filter = SteamGifCropper.Properties.Resources.FileDialog_GifFilter,
                    Title = SteamGifCropper.Properties.Resources.FileDialog_SaveReversedGif ?? "Save reversed GIF as",
                    FileName = Path.GetFileName(outputFilePath)
                })
                {
                    if (saveFileDialog.ShowDialog() != DialogResult.OK) return;
                    outputFilePath = saveFileDialog.FileName;
                }

                SetStatusText(mainForm, SteamGifCropper.Properties.Resources.Status_ReversingGif ?? "Reversing GIF...");
                mainForm.pBarTaskStatus.Visible = true;
                SetProgressBar(mainForm.pBarTaskStatus, 0, mainForm.pBarTaskStatus.Maximum);

                mainForm.Enabled = false;
                try
                {
                    // Check FFmpeg availability
                    var (isAvailable, ffmpegPath, version, error) = GetFFmpegDiagnostics();
                    if (!isAvailable)
                    {
                        throw new InvalidOperationException($"FFmpeg not available: {error}");
                    }

                    // Get target framerate from main form
                    int targetFramerate = (int)mainForm.numUpDownFramerate.Value;

                    SetProgressBar(mainForm.pBarTaskStatus, 25, mainForm.pBarTaskStatus.Maximum);
                    await Task.Delay(1);

                    // Use FFMpegCore to reverse the GIF
                    var inputAnalysis = await FFProbe.AnalyseAsync(inputFilePath);
                    var totalDuration = inputAnalysis.Duration;

                    SetProgressBar(mainForm.pBarTaskStatus, 50, mainForm.pBarTaskStatus.Maximum);
                    await Task.Delay(1);
                    // Reverse GIF directly with palettegen + paletteuse using streaming to limit memory usage
                    SetProgressBar(mainForm.pBarTaskStatus, 75, mainForm.pBarTaskStatus.Maximum);
                    await using var reverseInput = File.OpenRead(inputFilePath);
                    await using var reverseOutput = File.Open(outputFilePath, FileMode.Create, FileAccess.Write);
                    var token = CreateFfmpegCancellationToken();
                    await FFMpegArguments
                        .FromPipeInput(new StreamPipeSource(reverseInput))
                        .OutputToPipe(new StreamPipeSink(reverseOutput), options =>
                            {
                                options.ForceFormat("gif")
                                       .WithCustomArgument(
                                           @"-filter_complex ""[0:v]reverse,split[s0][s1];[s0]palettegen=stats_mode=single[p];[s1][p]paletteuse=dither=bayer:bayer_scale=3"""
                                       )
                                       .WithFramerate(targetFramerate);
                                ApplyThreadLimit(options);
                            })
                        .CancellableThrough(token)
                        .ProcessAsynchronously();

                    SetProgressBar(mainForm.pBarTaskStatus, 100, mainForm.pBarTaskStatus.Maximum);
                    await Task.Delay(1);

                    SetStatusText(mainForm, SteamGifCropper.Properties.Resources.Status_GifReversed ?? "GIF reversed successfully!");
                    WindowsThemeManager.ShowThemeAwareMessageBox(
                        mainForm,
                        (SteamGifCropper.Properties.Resources.Message_GifReversedSuccess ?? "GIF reversed successfully!") + $"\n{outputFilePath}",
                        SteamGifCropper.Properties.Resources.Title_Success ?? "Success",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    // Fallback to ImageMagick if FFmpeg fails
                    try
                    {
                        SetStatusText(mainForm, SteamGifCropper.Properties.Resources.Status_FFmpegFallback);
                        SetProgressBar(mainForm.pBarTaskStatus, 25, 100);

                        // Get target framerate from main form (UI thread, before Task.Run)
                        int fallbackFramerate = (int)mainForm.numUpDownFramerate.Value;

                        await Task.Run(() =>
                        {
                            using (var collection = new MagickImageCollection(inputFilePath))
                            {
                                SetProgressBar(mainForm.pBarTaskStatus, 50, 100);

                                // Reverse the frame order
                                collection.Reverse();

                                SetProgressBar(mainForm.pBarTaskStatus, 75, 100);

                                // Apply framerate setting to all frames
                                uint frameDelay = (uint)(100.0 / fallbackFramerate); // Convert fps to delay (in 1/100th seconds)
                                foreach (var frame in collection)
                                {
                                    frame.AnimationDelay = frameDelay;
                                }

                                SetProgressBar(mainForm.pBarTaskStatus, 90, 100);

                                collection.Write(outputFilePath);

                                SetProgressBar(mainForm.pBarTaskStatus, 100, 100);
                            }
                        });

                        SetStatusText(mainForm, SteamGifCropper.Properties.Resources.Status_GifReversed ?? "GIF reversed successfully!");
                        WindowsThemeManager.ShowThemeAwareMessageBox(
                            mainForm,
                            (SteamGifCropper.Properties.Resources.Message_GifReversedSuccess ?? "GIF reversed successfully!") + $"\n{outputFilePath}",
                            SteamGifCropper.Properties.Resources.Title_Success ?? "Success",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information);
                    }
                    catch (Exception fallbackEx)
                    {
                        SetStatusText(mainForm, SteamGifCropper.Properties.Resources.Status_Error ?? "Error");
                        WindowsThemeManager.ShowThemeAwareMessageBox(
                            mainForm,
                            string.Format(SteamGifCropper.Properties.Resources.Error_GifReverseFailed ?? "Failed to reverse GIF: {0}", $"FFmpeg: {ex.Message}, ImageMagick: {fallbackEx.Message}"),
                            SteamGifCropper.Properties.Resources.Title_Error ?? "Error",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error);
                    }
                }
                finally
                {
                    mainForm.Enabled = true;
                    SetProgressBar(mainForm.pBarTaskStatus, 0, mainForm.pBarTaskStatus.Maximum);
                    //mainForm.pBarTaskStatus.Visible = false;
                    SetStatusText(mainForm, SteamGifCropper.Properties.Resources.Status_Ready ?? "Ready");
                }
            }
        }

        public static void ScrollStaticImage(string inputFilePath, string outputFilePath,
            ScrollDirection direction, int stepPixels, int durationSeconds, bool fullCycle, int moveCount, int targetFramerate = 15)
        {
            ImageInputValidator.ValidateImage(inputFilePath);
            using var baseImage = new MagickImage(inputFilePath);
            int width = (int)baseImage.Width;
            int height = (int)baseImage.Height;

            int distance = direction switch
            {
                ScrollDirection.Up or ScrollDirection.Down => height,
                _ => width
            };

            int signX = 0, signY = 0;
            switch (direction)
            {
                case ScrollDirection.Right: signX = 1; break;
                case ScrollDirection.Left: signX = -1; break;
                case ScrollDirection.Down: signY = 1; break;
                case ScrollDirection.Up: signY = -1; break;
                case ScrollDirection.LeftUp: signX = -1; signY = -1; break;
                case ScrollDirection.LeftDown: signX = -1; signY = 1; break;
                case ScrollDirection.RightUp: signX = 1; signY = -1; break;
                case ScrollDirection.RightDown: signX = 1; signY = 1; break;
            }

            int frames;
            int dx = 0, dy = 0;
            double stepX = 0, stepY = 0;
            if (durationSeconds > 0)
            {
                frames = Math.Max(1, durationSeconds * targetFramerate);
                // Calculate separate steps for X and Y axes
                if (signX != 0) stepX = (double)width / frames;
                if (signY != 0) stepY = (double)height / frames;
            }
            else
            {
                dx = signX * stepPixels;
                dy = signY * stepPixels;
                if (fullCycle)
                {
                    int stepsX = dx != 0 ? (int)Math.Ceiling((double)width / Math.Abs(dx)) : 0;
                    int stepsY = dy != 0 ? (int)Math.Ceiling((double)height / Math.Abs(dy)) : 0;
                    frames = Math.Max(stepsX, stepsY);
                    if (frames <= 0) frames = 1;
                }
                else
                {
                    int maxMoves = moveCount;
                    if (dx != 0)
                        maxMoves = Math.Min(maxMoves, width / Math.Abs(dx));
                    if (dy != 0)
                        maxMoves = Math.Min(maxMoves, height / Math.Abs(dy));
                    frames = Math.Max(1, maxMoves);
                }
            }

            // Use simple delay calculation for scroll animation
            uint delay = (uint)Math.Round(100.0 / targetFramerate);

            if (File.Exists(outputFilePath))
                File.Delete(outputFilePath);

            using var collection = new MagickImageCollection();

            for (int i = 0; i < frames; i++)
            {
                var frame = baseImage.Clone();
                int offsetX, offsetY;
                if (durationSeconds > 0)
                {
                    offsetX = signX * (int)Math.Round(stepX * i);
                    offsetY = signY * (int)Math.Round(stepY * i);
                }
                else
                {
                    offsetX = dx * i;
                    offsetY = dy * i;
                }
                if (width > 0)
                {
                    offsetX %= width;
                    if (offsetX < 0) offsetX += width;
                }
                if (height > 0)
                {
                    offsetY %= height;
                    if (offsetY < 0) offsetY += height;
                }
                frame.Roll(offsetX, offsetY);
                frame.AnimationDelay = delay;
                frame.GifDisposeMethod = GifDisposeMethod.Background;

                collection.Add(frame);
            }

            var defines = new GifWriteDefines
            {
                RepeatCount = 0
            };

            collection.Write(outputFilePath, defines);
        }

        public static void ScrollStaticImage(string inputFilePath, string outputFilePath,
            ScrollDirection direction, int stepPixels, int durationSeconds, bool fullCycle, int moveCount, int targetFramerate,
            GifToolMainForm mainForm)
        {
            ImageInputValidator.ValidateImage(inputFilePath);
            using var baseImage = new MagickImage(inputFilePath);
            int width = (int)baseImage.Width;
            int height = (int)baseImage.Height;

            int signX = 0, signY = 0;
            switch (direction)
            {
                case ScrollDirection.Right: signX = 1; break;
                case ScrollDirection.Left: signX = -1; break;
                case ScrollDirection.Down: signY = 1; break;
                case ScrollDirection.Up: signY = -1; break;
                case ScrollDirection.LeftUp: signX = -1; signY = -1; break;
                case ScrollDirection.LeftDown: signX = -1; signY = 1; break;
                case ScrollDirection.RightUp: signX = 1; signY = -1; break;
                case ScrollDirection.RightDown: signX = 1; signY = 1; break;
            }

            int frames;
            int dx = 0, dy = 0;
            double stepX = 0, stepY = 0;
            if (durationSeconds > 0)
            {
                frames = Math.Max(1, durationSeconds * targetFramerate);
                // Calculate separate steps for X and Y axes
                if (signX != 0) stepX = (double)width / frames;
                if (signY != 0) stepY = (double)height / frames;
            }
            else
            {
                dx = signX * stepPixels;
                dy = signY * stepPixels;
                if (fullCycle)
                {
                    int stepsX = dx != 0 ? (int)Math.Ceiling((double)width / Math.Abs(dx)) : 0;
                    int stepsY = dy != 0 ? (int)Math.Ceiling((double)height / Math.Abs(dy)) : 0;
                    frames = Math.Max(stepsX, stepsY);
                    if (frames <= 0) frames = 1;
                }
                else
                {
                    int maxMoves = moveCount;
                    if (dx != 0)
                        maxMoves = Math.Min(maxMoves, width / Math.Abs(dx));
                    if (dy != 0)
                        maxMoves = Math.Min(maxMoves, height / Math.Abs(dy));
                    frames = Math.Max(1, maxMoves);
                }
            }

            // Use simple delay calculation for scroll animation
            uint delay = (uint)Math.Round(100.0 / targetFramerate);

            if (File.Exists(outputFilePath))
                File.Delete(outputFilePath);

            using var collection = new MagickImageCollection();

            mainForm?.Invoke((Action)(() =>
            {
                mainForm.pBarTaskStatus.Maximum = frames;
                SetProgressBar(mainForm.pBarTaskStatus, 0, mainForm.pBarTaskStatus.Maximum);
                SetStatusText(mainForm, string.Format("Creating scroll animation - Frame {0}/{1}", 0, frames));
            }));

            for (int i = 0; i < frames; i++)
            {
                var frame = baseImage.Clone();
                int offsetX, offsetY;
                if (durationSeconds > 0)
                {
                    offsetX = signX * (int)Math.Round(stepX * i);
                    offsetY = signY * (int)Math.Round(stepY * i);
                }
                else
                {
                    offsetX = dx * i;
                    offsetY = dy * i;
                }
                if (width > 0)
                {
                    offsetX %= width;
                    if (offsetX < 0) offsetX += width;
                }
                if (height > 0)
                {
                    offsetY %= height;
                    if (offsetY < 0) offsetY += height;
                }
                frame.Roll(offsetX, offsetY);
                frame.AnimationDelay = delay;
                frame.GifDisposeMethod = GifDisposeMethod.Background;

                collection.Add(frame);

                if (mainForm != null)
                {
                    int current = i + 1;
                    mainForm.Invoke((Action)(() =>
                    {
                        SetProgressBar(mainForm.pBarTaskStatus, current, mainForm.pBarTaskStatus.Maximum);
                        SetStatusText(mainForm, string.Format("Creating scroll animation - Frame {0}/{1}", current, frames));
                    }));
                }
            }

            var defines = new GifWriteDefines
            {
                RepeatCount = 0
            };

            if (mainForm != null)
            {
                mainForm.Invoke((Action)(() =>
                {
                    SetStatusText(mainForm, Resources.Status_Saving);                }));
            }

            collection.Write(outputFilePath, defines);

            if (mainForm != null)
            {
                mainForm.Invoke((Action)(() =>
                {
                    SetStatusText(mainForm, Resources.Status_Done);                }));
            }
        }

        public static async Task ScrollStaticImage(GifToolMainForm mainForm)
        {
            using var dialog = new ScrollStaticImageDialog(true);
            if (dialog.ShowDialog() != DialogResult.OK)
                return;

            string inputPath = dialog.InputFilePath;
            ImageInputValidator.ValidateImage(inputPath);
            string outputPath = dialog.OutputFilePath;
            ScrollDirection direction = dialog.Direction;
            int step = dialog.StepPixels;
            int duration = dialog.DurationSeconds;
            int moveCount = dialog.MoveCount;
            bool fullCycle = dialog.FullCycle;
            bool autoDuration = dialog.AutoDuration;
            int targetFramerate = (int)mainForm.numUpDownFramerate.Value;
            var gifsicle = CaptureGifsicleSnapshot(mainForm);

            // Auto-calculate duration if requested and input is GIF
            if (autoDuration && Path.GetExtension(inputPath).ToLowerInvariant() == ".gif")
            {
                try
                {
                    using var inputCollection = new MagickImageCollection(inputPath);
                    // Calculate total duration of one complete GIF cycle
                    double totalDurationSeconds = inputCollection.Sum(frame => (double)frame.AnimationDelay) / 100.0;
                    duration = (int)Math.Ceiling(totalDurationSeconds);

                    mainForm.Invoke((Action)(() =>
                    {
                        SetStatusText(mainForm, $"Auto-calculated GIF cycle duration: {totalDurationSeconds:F2}s → {duration}s");
                    }));
                }
                catch (Exception ex)
                {
                    mainForm.Invoke((Action)(() =>
                    {
                        SetStatusText(mainForm, $"Failed to calculate auto-duration: {ex.Message}");
                    }));
                    duration = 5; // Fallback to 5 seconds
                }
            }

            mainForm.Enabled = false;
            try
            {
                mainForm.pBarTaskStatus.Visible = true;
                SetProgressBar(mainForm.pBarTaskStatus, 0, mainForm.pBarTaskStatus.Maximum);
                SetStatusText(mainForm, SteamGifCropper.Properties.Resources.Status_Processing);

                await Task.Run(() => {
                    // Detect if input is GIF or static image
                    if (Path.GetExtension(inputPath).ToLowerInvariant() == ".gif")
                    {
                        ScrollAnimatedGif(inputPath, outputPath, direction, step, duration, fullCycle, moveCount, targetFramerate, mainForm, autoDuration);
                    }
                    else
                    {
                        ScrollStaticImage(inputPath, outputPath, direction, step, duration, fullCycle, moveCount, targetFramerate, mainForm);
                    }
                });

                await OptimizeWithGifsicleIfEnabled(mainForm, gifsicle, outputPath);

                SetProgressBar(mainForm.pBarTaskStatus, mainForm.pBarTaskStatus.Maximum, mainForm.pBarTaskStatus.Maximum);
                SetStatusText(mainForm, SteamGifCropper.Properties.Resources.Status_Done);
                WindowsThemeManager.ShowThemeAwareMessageBox(mainForm,
                                SteamGifCropper.Properties.Resources.Message_ProcessingComplete,
                                SteamGifCropper.Properties.Resources.Title_Success,
                                MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (MagickResourceLimitErrorException)
            {
                SetStatusText(mainForm, SteamGifCropper.Properties.Resources.Status_Error);
                WindowsThemeManager.ShowThemeAwareMessageBox(mainForm,
                                SteamGifCropper.Properties.Resources.Error_CacheResourcesExhausted,
                                SteamGifCropper.Properties.Resources.Title_Error,
                                MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                SetStatusText(mainForm, SteamGifCropper.Properties.Resources.Status_Error);
                WindowsThemeManager.ShowThemeAwareMessageBox(mainForm,
                                string.Format(SteamGifCropper.Properties.Resources.Error_Occurred, ex.Message),
                                SteamGifCropper.Properties.Resources.Title_Error,
                                MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                mainForm.Enabled = true;
                SetProgressBar(mainForm.pBarTaskStatus, 0, mainForm.pBarTaskStatus.Maximum);
                //mainForm.pBarTaskStatus.Visible = false;
                SetStatusText(mainForm, SteamGifCropper.Properties.Resources.Status_Ready);
            }
        }

        public static async Task ScrollAnimatedGif(GifToolMainForm mainForm)
        {
            using var dialog = new ScrollStaticImageDialog(true);
            if (dialog.ShowDialog() != DialogResult.OK)
                return;

            string inputPath = dialog.InputFilePath;
            ImageInputValidator.ValidateImage(inputPath);
            string outputPath = dialog.OutputFilePath;
            ScrollDirection direction = dialog.Direction;
            int step = dialog.StepPixels;
            int duration = dialog.DurationSeconds;
            int moveCount = dialog.MoveCount;
            bool fullCycle = dialog.FullCycle;
            bool autoDuration = dialog.AutoDuration;
            int targetFramerate = (int)mainForm.numUpDownFramerate.Value;
            var gifsicle = CaptureGifsicleSnapshot(mainForm);

            // Auto-calculate duration for GIF cycle
            if (autoDuration)
            {
                try
                {
                    using var inputCollection = new MagickImageCollection(inputPath);
                    // Calculate total duration of one complete GIF cycle
                    double totalDurationSeconds = inputCollection.Sum(frame => (double)frame.AnimationDelay) / 100.0;
                    duration = (int)Math.Ceiling(totalDurationSeconds);

                    mainForm.Invoke((Action)(() =>
                    {
                        SetStatusText(mainForm, $"Auto-calculated GIF cycle duration: {totalDurationSeconds:F2}s → {duration}s");
                    }));
                }
                catch (Exception ex)
                {
                    mainForm.Invoke((Action)(() =>
                    {
                        SetStatusText(mainForm, $"Failed to calculate auto-duration: {ex.Message}");
                    }));
                    duration = 5; // Fallback to 5 seconds
                }
            }

            mainForm.Enabled = false;
            try
            {
                mainForm.pBarTaskStatus.Visible = true;
                SetProgressBar(mainForm.pBarTaskStatus, 0, mainForm.pBarTaskStatus.Maximum);
                SetStatusText(mainForm, SteamGifCropper.Properties.Resources.Status_Processing);

                await Task.Run(() => {
                    // Detect if input is GIF or static image
                    if (Path.GetExtension(inputPath).ToLowerInvariant() == ".gif")
                    {
                        ScrollAnimatedGif(inputPath, outputPath, direction, step, duration, fullCycle, moveCount, targetFramerate, mainForm, autoDuration);
                    }
                    else
                    {
                        ScrollStaticImage(inputPath, outputPath, direction, step, duration, fullCycle, moveCount, targetFramerate, mainForm);
                    }
                });

                await OptimizeWithGifsicleIfEnabled(mainForm, gifsicle, outputPath);

                WindowsThemeManager.ShowThemeAwareMessageBox(mainForm,
                                SteamGifCropper.Properties.Resources.Message_ProcessingComplete,
                                SteamGifCropper.Properties.Resources.Title_Success,
                                MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (MagickResourceLimitErrorException)
            {
                SetStatusText(mainForm, SteamGifCropper.Properties.Resources.Status_Error);
                WindowsThemeManager.ShowThemeAwareMessageBox(mainForm,
                                SteamGifCropper.Properties.Resources.Error_CacheResourcesExhausted,
                                SteamGifCropper.Properties.Resources.Title_Error,
                                MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                SetStatusText(mainForm, SteamGifCropper.Properties.Resources.Status_Error);
                WindowsThemeManager.ShowThemeAwareMessageBox(mainForm,
                                string.Format(SteamGifCropper.Properties.Resources.Error_Occurred, ex.Message),
                                SteamGifCropper.Properties.Resources.Title_Error,
                                MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                mainForm.Enabled = true;
                SetProgressBar(mainForm.pBarTaskStatus, 0, mainForm.pBarTaskStatus.Maximum);
                SetStatusText(mainForm, SteamGifCropper.Properties.Resources.Status_Ready);
            }
        }

        public static void ScrollAnimatedGif(string inputFilePath, string outputFilePath,
            ScrollDirection direction, int stepPixels, int durationSeconds, bool fullCycle, int moveCount, int targetFramerate, GifToolMainForm mainForm, bool autoDuration = false)
        {
            ImageInputValidator.ValidateGif(inputFilePath);
            // Memory usage estimation and validation
            var fileInfo = new FileInfo(inputFilePath);
            long fileSizeMB = fileInfo.Length / (1024 * 1024);

            // Warn user for large files
            if (fileSizeMB > 50)
            {
                DialogResult result = DialogResult.No;
                mainForm.Invoke((Action)(() =>
                {
                    result = WindowsThemeManager.ShowThemeAwareMessageBox(mainForm,
                        $"Large GIF file detected ({fileSizeMB}MB). Processing may use significant memory and time. Continue?",
                        "Memory Warning",
                        MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                }));
                if (result == DialogResult.No)
                    return;
            }

            using var inputCollection = new MagickImageCollection(inputFilePath);
            inputCollection.Coalesce();

            // Debug: Show input GIF information
            mainForm.Invoke((Action)(() =>
            {
                SetStatusText(mainForm, $"Input GIF: {inputCollection.Count} frames, {inputCollection[0].Width}x{inputCollection[0].Height}, delays: {string.Join(",", inputCollection.Take(5).Select(f => f.AnimationDelay))}");
            }));

            // Validate total estimated frames to prevent memory explosion
            int estimatedScrollFrames;
            if (autoDuration)
            {
                // For auto-duration, use the GIF frame count directly
                estimatedScrollFrames = inputCollection.Count;
            }
            else
            {
                estimatedScrollFrames = EstimateScrollFrames(direction, stepPixels, durationSeconds, fullCycle, moveCount,
                    (int)inputCollection[0].Width, (int)inputCollection[0].Height, targetFramerate);
            }
            long totalEstimatedFrames = (long)estimatedScrollFrames * inputCollection.Count;

            // Show diagnostic info
            mainForm.Invoke((Action)(() =>
            {
                SetStatusText(mainForm, $"Processing: {inputCollection.Count} original frames × {estimatedScrollFrames} scroll positions = {totalEstimatedFrames} output frames");
            }));

            // Apply different limits for auto-duration vs manual modes
            int frameLimit = autoDuration ? 500000 : 100000; // More generous limit for auto-duration
            if (totalEstimatedFrames > frameLimit)
            {
                string modeInfo = autoDuration ? "auto-duration" : "manual";
                throw new InvalidOperationException($"Too many frames would be generated ({totalEstimatedFrames}) in {modeInfo} mode.\n" +
                    $"Original frames: {inputCollection.Count}\n" +
                    $"Scroll positions: {estimatedScrollFrames}\n" +
                    $"Please reduce scroll duration, increase step size, or reduce move count.");
            }

            // Get original GIF properties
            var firstFrame = inputCollection[0];
            int originalWidth = (int)firstFrame.Width;
            int originalHeight = (int)firstFrame.Height;
            int originalDelay = (int)inputCollection[0].AnimationDelay;

            int distance = direction switch
            {
                ScrollDirection.Up or ScrollDirection.Down => originalHeight,
                _ => originalWidth
            };

            int signX = 0, signY = 0;
            switch (direction)
            {
                case ScrollDirection.Right: signX = 1; break;
                case ScrollDirection.Left: signX = -1; break;
                case ScrollDirection.Down: signY = 1; break;
                case ScrollDirection.Up: signY = -1; break;
                case ScrollDirection.LeftUp: signX = -1; signY = -1; break;
                case ScrollDirection.LeftDown: signX = -1; signY = 1; break;
                case ScrollDirection.RightUp: signX = 1; signY = -1; break;
                case ScrollDirection.RightDown: signX = 1; signY = 1; break;
            }

            int scrollFrames;
            int dx = 0, dy = 0;
            double step = 0;
            if (durationSeconds > 0)
            {
                // For duration-based scrolling, calculate frames to exactly cover the distance
                scrollFrames = durationSeconds * targetFramerate;
                step = (double)distance / scrollFrames;
            }
            else
            {
                dx = signX * stepPixels;
                dy = signY * stepPixels;
                if (fullCycle)
                {
                    int stepsX = dx != 0 ? (int)Math.Ceiling((double)originalWidth / Math.Abs(dx)) : 0;
                    int stepsY = dy != 0 ? (int)Math.Ceiling((double)originalHeight / Math.Abs(dy)) : 0;
                    scrollFrames = Math.Max(stepsX, stepsY);
                    if (scrollFrames <= 0) scrollFrames = 1;
                }
                else
                {
                    scrollFrames = moveCount;
                }
            }

            int frameDelay = Math.Max(1, 100 / targetFramerate);

            // Calculate original GIF timing
            double originalFPS = 100.0 / inputCollection[0].AnimationDelay; // AnimationDelay is in 1/100 seconds
            int totalScrollDurationMs = durationSeconds * 1000;

            // Calculate how much to scroll per original frame for X and Y separately
            double scrollPixelsPerFrameX = 0;
            double scrollPixelsPerFrameY = 0;

            if (autoDuration)
            {
                // For auto-duration, scroll exactly one full distance over the GIF's frame count
                if (signX != 0) scrollPixelsPerFrameX = (double)originalWidth / inputCollection.Count;
                if (signY != 0) scrollPixelsPerFrameY = (double)originalHeight / inputCollection.Count;
            }
            else if (durationSeconds > 0)
            {
                if (signX != 0) scrollPixelsPerFrameX = (double)originalWidth / (originalFPS * durationSeconds);
                if (signY != 0) scrollPixelsPerFrameY = (double)originalHeight / (originalFPS * durationSeconds);
            }

            // Calculate how long the scroll animation should last in terms of original frames
            int scrollAnimationFrames;
            if (autoDuration)
            {
                // For auto-duration, use the exact number of frames in the original GIF
                scrollAnimationFrames = inputCollection.Count;
            }
            else if (durationSeconds > 0)
            {
                scrollAnimationFrames = (int)(originalFPS * durationSeconds);
            }
            else
            {
                scrollAnimationFrames = scrollFrames;
            }

            // Use collection approach for proper GIF animation
            using var outputCollection = new MagickImageCollection();

            // Debug: Show scroll parameters
            mainForm.Invoke((Action)(() =>
            {
                string modeInfo = autoDuration ? "Auto-duration mode" : $"Manual {durationSeconds}s";
                SetStatusText(mainForm, $"{modeInfo}: {scrollAnimationFrames} frames, X:{scrollPixelsPerFrameX:F2}px/frame, Y:{scrollPixelsPerFrameY:F2}px/frame");
            }));

            double accumulatedScrollX = 0;
            double accumulatedScrollY = 0;
            int outputFrameCount = 0;

            // Phase 1: Scrolling animation with original GIF playing
            for (int scrollFrame = 0; scrollFrame < scrollAnimationFrames; scrollFrame++)
            {
                // Calculate which original frame to use (cycle through the original animation)
                int originalFrameIndex = scrollFrame % inputCollection.Count;
                var originalFrame = inputCollection[originalFrameIndex];

                // Calculate current accumulated scroll offset
                if (autoDuration || durationSeconds > 0)
                {
                    accumulatedScrollX += scrollPixelsPerFrameX * signX;
                    accumulatedScrollY += scrollPixelsPerFrameY * signY;
                }
                else
                {
                    accumulatedScrollX = dx * scrollFrame;
                    accumulatedScrollY = dy * scrollFrame;
                }

                // Create scrolled version of this frame
                var scrolledFrame = new MagickImage(MagickColors.Transparent, (uint)originalWidth, (uint)originalHeight);
                scrolledFrame.Format = MagickFormat.Gif;

                using var temp = originalFrame.Clone();
                temp.Roll((int)accumulatedScrollX, (int)accumulatedScrollY);

                scrolledFrame.Composite(temp, 0, 0, CompositeOperator.Over);
                scrolledFrame.AnimationDelay = originalFrame.AnimationDelay; // Use original frame timing
                scrolledFrame.GifDisposeMethod = GifDisposeMethod.Background;

                outputCollection.Add(scrolledFrame);
                outputFrameCount++;

                // Update progress
                int progress = (int)((double)(scrollFrame + 1) / scrollAnimationFrames * 70); // 70% for scrolling
                mainForm.Invoke((Action)(() =>
                {
                    SetProgressBar(mainForm.pBarTaskStatus, progress, 100);
                }));
            }

            // Phase 2: Continue normal animation after scroll completes
            // Calculate final scroll position
            int finalScrollX = (int)accumulatedScrollX;
            int finalScrollY = (int)accumulatedScrollY;

            if (fullCycle)
            {
                // For full cycle, the final position should maintain the wrap-around effect
                // Don't reset to 0 - keep the modulo position to show the scroll effect
                finalScrollX = finalScrollX % originalWidth;
                finalScrollY = finalScrollY % originalHeight;
                if (finalScrollX < 0) finalScrollX += originalWidth;
                if (finalScrollY < 0) finalScrollY += originalHeight;

                // Debug: Show final position for full cycle
                mainForm.Invoke((Action)(() =>
                {
                    SetStatusText(mainForm, $"Full cycle final position: X={finalScrollX}, Y={finalScrollY} (not reset to 0)");
                }));
            }

            // Phase 2: Continue with normal animation from the correct timeline position
            if (autoDuration)
            {
                // For auto-duration, we skip Phase 2 since we already played the complete cycle
                // The output should have exactly the same number of frames as the input
                mainForm.Invoke((Action)(() =>
                {
                    SetStatusText(mainForm, $"Auto-duration complete: {outputCollection.Count} frames (same as source)");
                }));
            }
            else if (durationSeconds > 0)
            {
                // For manual duration-based scrolling, continue from where the animation timeline left off
                int lastUsedFrameIndex = (scrollAnimationFrames - 1) % inputCollection.Count;
                int nextFrameIndex = (lastUsedFrameIndex + 1) % inputCollection.Count;

                // Debug: Show timeline continuity
                mainForm.Invoke((Action)(() =>
                {
                    SetStatusText(mainForm, $"Timeline continuity: Last used frame {lastUsedFrameIndex}, continuing from frame {nextFrameIndex}");
                }));

                // Continue the animation from the next frame in sequence
                // Play the remaining frames to complete the current animation cycle
                int remainingFrames = inputCollection.Count - (nextFrameIndex == 0 ? 0 : nextFrameIndex);
                if (nextFrameIndex == 0) remainingFrames = inputCollection.Count; // Full cycle if we're back at start

                for (int i = 0; i < remainingFrames; i++)
                {
                    int frameIndex = (nextFrameIndex + i) % inputCollection.Count;
                    var originalFrame = inputCollection[frameIndex];
                    var continueFrame = new MagickImage(MagickColors.Transparent, (uint)originalWidth, (uint)originalHeight);
                    continueFrame.Format = MagickFormat.Gif;

                    using var temp = originalFrame.Clone();
                    // Always apply final scroll position for both full cycle and non-full cycle
                    temp.Roll(finalScrollX, finalScrollY);

                    continueFrame.Composite(temp, 0, 0, CompositeOperator.Over);
                    continueFrame.AnimationDelay = originalFrame.AnimationDelay;
                    continueFrame.GifDisposeMethod = GifDisposeMethod.Background;

                    outputCollection.Add(continueFrame);
                    outputFrameCount++;

                    // Update progress
                    int progress = 70 + (int)((double)(i + 1) / remainingFrames * 30);
                    mainForm.Invoke((Action)(() =>
                    {
                        SetProgressBar(mainForm.pBarTaskStatus, progress, 100);
                    }));
                }
            }
            else if (!autoDuration)
            {
                // For step-based scrolling, calculate timeline continuity based on scroll duration
                // The scroll took 'scrollFrames' steps, each using one frame of original animation
                int lastUsedFrameIndex = (scrollFrames - 1) % inputCollection.Count;
                int nextFrameIndex = (lastUsedFrameIndex + 1) % inputCollection.Count;

                // Debug: Show step-based timeline continuity
                mainForm.Invoke((Action)(() =>
                {
                    SetStatusText(mainForm, $"Step-based continuity: Used {scrollFrames} scroll frames, last frame {lastUsedFrameIndex}, continuing from frame {nextFrameIndex}");
                }));

                // Continue the animation from the next frame in sequence
                int remainingFrames = inputCollection.Count - (nextFrameIndex == 0 ? 0 : nextFrameIndex);
                if (nextFrameIndex == 0) remainingFrames = inputCollection.Count; // Full cycle if we're back at start

                for (int i = 0; i < remainingFrames; i++)
                {
                    int frameIndex = (nextFrameIndex + i) % inputCollection.Count;
                    var originalFrame = inputCollection[frameIndex];
                    var continueFrame = new MagickImage(MagickColors.Transparent, (uint)originalWidth, (uint)originalHeight);
                    continueFrame.Format = MagickFormat.Gif;

                    using var temp = originalFrame.Clone();
                    // Always apply final scroll position for both full cycle and non-full cycle
                    temp.Roll(finalScrollX, finalScrollY);

                    continueFrame.Composite(temp, 0, 0, CompositeOperator.Over);
                    continueFrame.AnimationDelay = originalFrame.AnimationDelay;
                    continueFrame.GifDisposeMethod = GifDisposeMethod.Background;

                    outputCollection.Add(continueFrame);
                    outputFrameCount++;

                    // Update progress
                    int progress = 70 + (int)((double)(i + 1) / remainingFrames * 30);
                    mainForm.Invoke((Action)(() =>
                    {
                        SetProgressBar(mainForm.pBarTaskStatus, progress, 100);
                    }));
                }
            }

            // Write the complete collection to file
            mainForm.Invoke((Action)(() =>
            {
                SetStatusText(mainForm, $"Writing output GIF with {outputCollection.Count} frames...");
            }));

            outputCollection.Write(outputFilePath);

            // Debug: Show output info
            mainForm.Invoke((Action)(() =>
            {
                SetStatusText(mainForm, $"Output GIF created: {outputCollection.Count} frames, delays: {string.Join(",", outputCollection.Take(5).Select(f => f.AnimationDelay))}");
            }));

            mainForm.Invoke((Action)(() =>
            {
                SetStatusText(mainForm, Resources.Status_Done);
            }));
        }

        private static int EstimateScrollFrames(ScrollDirection direction, int stepPixels, int durationSeconds,
            bool fullCycle, int moveCount, int width, int height, int targetFramerate)
        {
            int distance = direction switch
            {
                ScrollDirection.Up or ScrollDirection.Down => height,
                _ => width
            };

            if (durationSeconds > 0)
            {
                // For duration-based scrolling, just use the time calculation
                return Math.Max(1, durationSeconds * targetFramerate);
            }
            else if (fullCycle)
            {
                // For full cycle, calculate based on step size
                return Math.Max(1, distance / Math.Max(1, stepPixels));
            }
            else
            {
                // For move count, just return the count
                return moveCount;
            }
        }

        private static List<MagickImage> ResampleBaseFrames(MagickImageCollection baseCollection, MagickImageCollection overlayCollection)
        {
            var baseDelays = baseCollection.Select(f => (int)f.AnimationDelay).ToArray();
            int baseTotalDelay = baseDelays.Sum();
            var resampled = new List<MagickImage>(overlayCollection.Count);

            int overlayElapsed = 0;
            foreach (var overlayFrame in overlayCollection)
            {
                int startTime = baseTotalDelay == 0 ? 0 : overlayElapsed % baseTotalDelay;
                int cumulative = 0;
                int baseIndex = 0;
                for (int i = 0; i < baseDelays.Length; i++)
                {
                    cumulative += baseDelays[i];
                    if (startTime < cumulative)
                    {
                        baseIndex = i;
                        break;
                    }
                }

                resampled.Add((MagickImage)baseCollection[baseIndex].Clone());
                overlayElapsed += (int)overlayFrame.AnimationDelay;
            }

            return resampled;
        }

        private static Task ProcessStaticOverlay(GifToolMainForm mainForm, MagickImageCollection baseCollection,
            MagickImageCollection overlayCollection, MagickImageCollection resultCollection,
            int offsetX, int offsetY, bool resampleBase, int baseWidth, int baseHeight)
        {
            // Initialize progress bar
            if (mainForm != null)
            {
                SetProgressRange(mainForm, 0, 100);
                SetProgressBar(mainForm.pBarTaskStatus, 0, 100);
            }

            if (resampleBase)
            {
                var resampledBaseFrames = ResampleBaseFrames(baseCollection, overlayCollection);
                int overlayCount = overlayCollection.Count;

                SetStatusText(mainForm, $"Processing static overlay (resampled): 0/{overlayCount} frames");

                for (int i = 0; i < overlayCount; i++)
                {
                    using var baseFrame = resampledBaseFrames[i];
                    using var overlayFrame = overlayCollection[i].Clone();

                    // Handle partial visibility and bounds checking
                    var overlayGeometry = CalculateOverlayGeometry((MagickImage)overlayFrame, baseWidth, baseHeight, offsetX, offsetY);
                    if (overlayGeometry.Width <= 0 || overlayGeometry.Height <= 0)
                    {
                        // Overlay is completely out of bounds, add base frame only
                        resultCollection.Add(baseFrame.Clone());
                        continue;
                    }

                    // Crop overlay if it extends beyond base boundaries
                    if (overlayGeometry.CropRequired)
                    {
                        overlayFrame.Crop(new MagickGeometry(overlayGeometry.CropX, overlayGeometry.CropY,
                            (uint)overlayGeometry.Width, (uint)overlayGeometry.Height));
                        overlayFrame.Page = new MagickGeometry(0, 0, overlayFrame.Width, overlayFrame.Height);
                    }

                    baseFrame.Composite(overlayFrame, overlayGeometry.CompositeX, overlayGeometry.CompositeY, CompositeOperator.Over);
                    baseFrame.AnimationDelay = overlayFrame.AnimationDelay;
                    baseFrame.AnimationTicksPerSecond = overlayFrame.AnimationTicksPerSecond;
                    baseFrame.GifDisposeMethod = GifDisposeMethod.Background;

                    resultCollection.Add(baseFrame.Clone());
                    UpdateFrameProgress(mainForm, i + 1, overlayCount);
                }

                resampledBaseFrames.Clear();
            }
            else
            {
                int baseCount = baseCollection.Count;
                var overlayDelays = overlayCollection.Select(f => (int)f.AnimationDelay).ToArray();

                SetStatusText(mainForm, $"Processing static overlay: 0/{baseCount} frames");
                int overlayTotalDelay = overlayDelays.Sum();
                int baseElapsed = 0;

                for (int i = 0; i < baseCount; i++)
                {
                    using var baseFrame = (MagickImage)baseCollection[i].Clone();

                    int startTime = overlayTotalDelay == 0 ? 0 : baseElapsed % overlayTotalDelay;
                    int cumulative = 0;
                    int overlayIndex = 0;
                    for (int j = 0; j < overlayDelays.Length; j++)
                    {
                        cumulative += overlayDelays[j];
                        if (startTime < cumulative)
                        {
                            overlayIndex = j;
                            break;
                        }
                    }

                    using var overlayFrame = overlayCollection[overlayIndex].Clone();

                    // Handle partial visibility and bounds checking
                    var overlayGeometry = CalculateOverlayGeometry((MagickImage)overlayFrame, baseWidth, baseHeight, offsetX, offsetY);
                    if (overlayGeometry.Width <= 0 || overlayGeometry.Height <= 0)
                    {
                        // Overlay is completely out of bounds, add base frame only
                        baseElapsed += (int)baseCollection[i].AnimationDelay;
                        resultCollection.Add(baseFrame.Clone());
                        continue;
                    }

                    // Crop overlay if it extends beyond base boundaries
                    if (overlayGeometry.CropRequired)
                    {
                        overlayFrame.Crop(new MagickGeometry(overlayGeometry.CropX, overlayGeometry.CropY,
                            (uint)overlayGeometry.Width, (uint)overlayGeometry.Height));
                        overlayFrame.Page = new MagickGeometry(0, 0, overlayFrame.Width, overlayFrame.Height);
                    }

                    baseFrame.Composite(overlayFrame, overlayGeometry.CompositeX, overlayGeometry.CompositeY, CompositeOperator.Over);
                    baseFrame.GifDisposeMethod = GifDisposeMethod.Background;

                    resultCollection.Add(baseFrame.Clone());

                    baseElapsed += (int)baseCollection[i].AnimationDelay;
                    UpdateFrameProgress(mainForm, i + 1, baseCount);
                }
            }
            return Task.CompletedTask;
        }

        private static (int signX, int signY) GetDirectionSigns(ScrollDirection direction)
        {
            int signX = 0, signY = 0;
            switch (direction)
            {
                case ScrollDirection.Right: signX = 1; break;
                case ScrollDirection.Left: signX = -1; break;
                case ScrollDirection.Down: signY = 1; break;
                case ScrollDirection.Up: signY = -1; break;
                case ScrollDirection.LeftUp: signX = -1; signY = -1; break;
                case ScrollDirection.LeftDown: signX = -1; signY = 1; break;
                case ScrollDirection.RightUp: signX = 1; signY = -1; break;
                case ScrollDirection.RightDown: signX = 1; signY = 1; break;
            }
            return (signX, signY);
        }

        private static Task ProcessMovingOverlay(GifToolMainForm mainForm, MagickImageCollection baseCollection,
            MagickImageCollection overlayCollection, MagickImageCollection resultCollection,
            ScrollDirection direction, int stepPixels, int moveCount, bool infiniteMovement,
            bool resampleBase, int baseWidth, int baseHeight, int overlayWidth, int overlayHeight,
            int startX, int startY)
        {
            // Calculate movement direction vectors
            (int signX, int signY) = GetDirectionSigns(direction);

            // Calculate movement parameters
            int totalFrames;
            if (infiniteMovement)
            {
                // Match base GIF duration
                totalFrames = baseCollection.Count;
            }
            else
            {
                // Use specified move count
                totalFrames = moveCount;
            }

            // Starting position is provided from Static Overlay Position coordinates
            // Movement will begin from these coordinates and proceed in the specified direction

            // Initialize progress bar
            if (mainForm != null)
            {
                SetProgressRange(mainForm, 0, 100);
                SetProgressBar(mainForm.pBarTaskStatus, 0, 100);
            }

            SetStatusText(mainForm, $"Processing moving overlay: 0/{totalFrames} frames");

            for (int frame = 0; frame < totalFrames; frame++)
            {
                // Calculate current overlay position
                int currentX = startX + (signX * stepPixels * frame);
                int currentY = startY + (signY * stepPixels * frame);

                // Get corresponding base and overlay frames
                var baseFrame = baseCollection[frame % baseCollection.Count].Clone();
                var overlayFrame = overlayCollection[frame % overlayCollection.Count].Clone();

                // Handle partial visibility and bounds checking
                var overlayGeometry = CalculateOverlayGeometry((MagickImage)overlayFrame, baseWidth, baseHeight, currentX, currentY);

                if (overlayGeometry.Width > 0 && overlayGeometry.Height > 0)
                {
                    // Overlay is at least partially visible
                    if (overlayGeometry.CropRequired)
                    {
                        overlayFrame.Crop(new MagickGeometry(overlayGeometry.CropX, overlayGeometry.CropY,
                            (uint)overlayGeometry.Width, (uint)overlayGeometry.Height));
                        overlayFrame.Page = new MagickGeometry(0, 0, overlayFrame.Width, overlayFrame.Height);
                    }

                    baseFrame.Composite(overlayFrame, overlayGeometry.CompositeX, overlayGeometry.CompositeY, CompositeOperator.Over);
                }

                baseFrame.GifDisposeMethod = GifDisposeMethod.Background;
                resultCollection.Add(baseFrame);

                overlayFrame.Dispose();
                UpdateFrameProgress(mainForm, frame + 1, totalFrames);
            }
            return Task.CompletedTask;
        }

        private struct OverlayGeometry
        {
            public int Width;
            public int Height;
            public int CompositeX;
            public int CompositeY;
            public int CropX;
            public int CropY;
            public bool CropRequired;
        }

        private static OverlayGeometry CalculateOverlayGeometry(MagickImage overlayFrame, int baseWidth, int baseHeight, int offsetX, int offsetY)
        {
            var geometry = new OverlayGeometry();
            int overlayWidth = (int)overlayFrame.Width;
            int overlayHeight = (int)overlayFrame.Height;

            // Calculate intersection with base boundaries
            int leftBound = Math.Max(0, offsetX);
            int topBound = Math.Max(0, offsetY);
            int rightBound = Math.Min(baseWidth, offsetX + overlayWidth);
            int bottomBound = Math.Min(baseHeight, offsetY + overlayHeight);

            geometry.Width = Math.Max(0, rightBound - leftBound);
            geometry.Height = Math.Max(0, bottomBound - topBound);

            if (geometry.Width <= 0 || geometry.Height <= 0)
            {
                // Completely out of bounds
                return geometry;
            }

            // Determine if cropping is needed
            geometry.CropRequired = (offsetX < 0 || offsetY < 0 || offsetX + overlayWidth > baseWidth || offsetY + overlayHeight > baseHeight);

            if (geometry.CropRequired)
            {
                // Calculate crop coordinates within overlay image
                geometry.CropX = Math.Max(0, -offsetX);
                geometry.CropY = Math.Max(0, -offsetY);
            }

            // Calculate composite position (always >= 0)
            geometry.CompositeX = Math.Max(0, offsetX);
            geometry.CompositeY = Math.Max(0, offsetY);

            return geometry;
        }

        public static async Task OverlayGif(GifToolMainForm mainForm)
        {
            using var dialog = new OverlayGifDialog();
            if (dialog.ShowDialog(mainForm) != DialogResult.OK)
                return;

            string basePath = dialog.BaseGifPath;
            string overlayPath = dialog.OverlayGifPath;
            string outputPath = dialog.OutputGifPath;
            ImageInputValidator.ValidateGif(basePath);
            ImageInputValidator.ValidateGif(overlayPath);
            bool resampleBase = dialog.ResampleBaseFrames;

            // Capture every dialog value on the UI thread; the dialog is a control and must not be
            // touched from the background worker.
            bool useStaticOverlay = dialog.UseStaticOverlay;
            int staticOverlayX = dialog.StaticOverlayX;
            int staticOverlayY = dialog.StaticOverlayY;
            var movementDirection = dialog.MovementDirection;
            int stepPixels = dialog.StepPixels;
            int moveCount = dialog.MoveCount;
            bool infiniteMovement = dialog.InfiniteMovement;
            var gifsicle = CaptureGifsicleSnapshot(mainForm);

            mainForm.Enabled = false;
            SetProgressRange(mainForm, 0, 100);
            SetProgressBar(mainForm.pBarTaskStatus, 0, 100);
            try
            {
                SetStatusText(mainForm, SteamGifCropper.Properties.Resources.Status_Loading);

                await Task.Run(async () =>
                {
                    using var baseCollection = new MagickImageCollection(basePath);
                    using var overlayCollection = new MagickImageCollection(overlayPath);
                    using var resultCollection = new MagickImageCollection();

                    int baseWidth = (int)baseCollection[0].Width;
                    int baseHeight = (int)baseCollection[0].Height;
                    int overlayWidth = (int)overlayCollection[0].Width;
                    int overlayHeight = (int)overlayCollection[0].Height;

                    baseCollection.Coalesce();
                    overlayCollection.Coalesce();

                    if (useStaticOverlay)
                    {
                        // Static overlay - use original logic with fixed position
                        await ProcessStaticOverlay(mainForm, baseCollection, overlayCollection, resultCollection,
                            staticOverlayX, staticOverlayY, resampleBase, baseWidth, baseHeight);
                    }
                    else
                    {
                        // Moving overlay - new logic starting from static overlay position
                        await ProcessMovingOverlay(mainForm, baseCollection, overlayCollection, resultCollection,
                            movementDirection, stepPixels, moveCount, infiniteMovement,
                            resampleBase, baseWidth, baseHeight, overlayWidth, overlayHeight,
                            staticOverlayX, staticOverlayY);
                    }

                    resultCollection.Quantize();
                    resultCollection.Optimize();

                    SetStatusText(mainForm, SteamGifCropper.Properties.Resources.Status_Saving);
                    resultCollection.Write(outputPath);
                });

                await OptimizeWithGifsicleIfEnabled(mainForm, gifsicle, outputPath);

                SetStatusText(mainForm, SteamGifCropper.Properties.Resources.Status_Done);
                WindowsThemeManager.ShowThemeAwareMessageBox(mainForm,
                    SteamGifCropper.Properties.Resources.Message_OverlayComplete,
                    SteamGifCropper.Properties.Resources.Title_Success,
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                SetStatusText(mainForm, SteamGifCropper.Properties.Resources.Status_Error);
                WindowsThemeManager.ShowThemeAwareMessageBox(mainForm,
                    $"Error: {ex.Message}",
                    SteamGifCropper.Properties.Resources.Title_Error,
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                mainForm.Enabled = true;
                SetProgressBar(mainForm.pBarTaskStatus, 0, 100);
                SetStatusText(mainForm, SteamGifCropper.Properties.Resources.Status_Idle);
            }
        }

        #region GIF Concatenation Methods

        public static async Task ConcatenateGifs(GifToolMainForm mainForm, GifConcatenationSettings settings)
        {
            if (settings.GifFilePaths == null || settings.GifFilePaths.Count < 2)
            {
                WindowsThemeManager.ShowThemeAwareMessageBox(mainForm,
                    "Please select at least 2 GIF files to concatenate.",
                    SteamGifCropper.Properties.Resources.Title_Error,
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Validate all files are genuine GIFs
            ImageInputValidator.ValidateGifs(settings.GifFilePaths);

            // Validate all files exist
            foreach (string filePath in settings.GifFilePaths)
            {
                if (!File.Exists(filePath))
                {
                    WindowsThemeManager.ShowThemeAwareMessageBox(mainForm,
                        string.Format(SteamGifCropper.Properties.Resources.MergeDialog_FileNotFound, 
                                     Path.GetFileName(filePath)),
                        SteamGifCropper.Properties.Resources.Title_Error,
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }

            var gifCollections = new List<MagickImageCollection>();
            MagickImageCollection result = null;

            // gifsicle gates on the dialog's own flag, not chkGifsicle, but still honors the shared
            // size threshold + lossy/palette/optimize/dither controls.
            var gifsicle = CaptureGifsicleSnapshot(mainForm, settings.UseGifsicleOptimization, true);
            mainForm.Enabled = false;
            try
            {
                SetProgressRange(mainForm, 0, 100);
                SetProgressBar(mainForm.pBarTaskStatus, 0, 100);
                SetStatusText(mainForm, SteamGifCropper.Properties.Resources.Status_Loading);

                // Steps 1-9 are CPU/IO-heavy → run on a background thread. All progress updates use the
                // marshaled SetProgressBar/SetStatusText helpers; none of the called methods touch controls.
                await Task.Run(async () =>
                {
                    // Step 1: Load all GIF files
                    for (int i = 0; i < settings.GifFilePaths.Count; i++)
                    {
                        MagickImageCollection collection = null;
                        try
                        {
                            collection = new MagickImageCollection(settings.GifFilePaths[i]);
                            collection.Coalesce();
                            gifCollections.Add(collection);
                            collection = null; // Ownership transferred to list
                        }
                        finally
                        {
                            // Dispose only if not added to list
                            collection?.Dispose();
                        }

                        SetProgressBar(mainForm.pBarTaskStatus, (i + 1) * 10 / settings.GifFilePaths.Count, 100);
                    }

                    SetProgressBar(mainForm.pBarTaskStatus, 15, 100);
                    SetStatusText(mainForm, "Analyzing GIF properties...");

                    // Step 2: Analyze GIF properties
                    var analysis = AnalyzeGifProperties(gifCollections);
                    SetProgressBar(mainForm.pBarTaskStatus, 25, 100);

                    // Step 3: Unify FPS
                    SetStatusText(mainForm, "Unifying frame rates...");
                    await UnifyFrameRates(gifCollections, settings, analysis);
                    SetProgressBar(mainForm.pBarTaskStatus, 40, 100);

                    // Step 4: Unify dimensions if requested
                    if (settings.UnifyDimensions)
                    {
                        SetStatusText(mainForm, "Unifying dimensions...");
                        UnifyDimensions(gifCollections, analysis);
                        SetProgressBar(mainForm.pBarTaskStatus, 55, 100);
                    }

                    // Step 5: Build unified palette
                    SetStatusText(mainForm, "Building unified palette...");
                    var unifiedPalette = BuildUnifiedPalette(gifCollections, settings);
                    SetProgressBar(mainForm.pBarTaskStatus, 65, 100);

                    // Step 6: Apply unified palette
                    SetStatusText(mainForm, "Applying unified palette...");
                    await ApplyUnifiedPalette(gifCollections, unifiedPalette, settings.UseFasterPalette);
                    SetProgressBar(mainForm.pBarTaskStatus, 80, 100);

                    // Step 7: Generate transitions and concatenate GIFs
                    SetStatusText(mainForm, "Generating transitions and concatenating GIF files...");
                    var transitionProgress = new Progress<(int current, int total, string status)>(report =>
                    {
                        // Map transition progress to overall progress (80-90%)
                        int overallProgress = 80 + (report.current * 10 / report.total);
                        SetProgressBar(mainForm.pBarTaskStatus, overallProgress, 100);
                        SetStatusText(mainForm, report.status);
                    });

                    result = await ConcatenateGifCollectionsWithTransitions(gifCollections, settings, analysis.MaxFps, transitionProgress);
                    SetProgressBar(mainForm.pBarTaskStatus, 90, 100);

                    // Step 8: Save result
                    SetStatusText(mainForm, SteamGifCropper.Properties.Resources.Status_Saving);
                    result.Write(settings.OutputFilePath);
                    SetProgressBar(mainForm.pBarTaskStatus, 95, 100);

                    // Step 9: Optional gifsicle optimization (gated + size-thresholded)
                    await OptimizeWithGifsicleIfEnabled(mainForm, gifsicle, settings.OutputFilePath);

                    SetProgressBar(mainForm.pBarTaskStatus, 100, 100);
                });

                SetStatusText(mainForm, SteamGifCropper.Properties.Resources.Status_Done);

                WindowsThemeManager.ShowThemeAwareMessageBox(mainForm,
                    string.Format("GIF concatenation completed successfully!\nSaved as: {0}", settings.OutputFilePath),
                    SteamGifCropper.Properties.Resources.Title_Success,
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                SetStatusText(mainForm, SteamGifCropper.Properties.Resources.Status_Error);
                WindowsThemeManager.ShowThemeAwareMessageBox(mainForm,
                    string.Format(SteamGifCropper.Properties.Resources.Error_Processing, ex.Message),
                    SteamGifCropper.Properties.Resources.Title_Error,
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                mainForm.Enabled = true;

                // Cleanup all collections and result to prevent memory leaks
                foreach (var collection in gifCollections)
                {
                    try { collection?.Dispose(); }
                    catch { /* Log disposal error */ }
                }

                try { result?.Dispose(); }
                catch { /* Log disposal error */ }
            }
        }

        private static GifPropertyAnalysis AnalyzeGifProperties(List<MagickImageCollection> gifCollections)
        {
            var analysis = new GifPropertyAnalysis();

            foreach (var collection in gifCollections)
            {
                if (collection.Count > 0)
                {
                    var fps = GetGifFrameRate(collection);
                    var dimensions = new { Width = collection[0].Width, Height = collection[0].Height };

                    analysis.FrameRates.Add(fps);
                    analysis.Dimensions.Add(dimensions);
                    
                    if (fps > analysis.MaxFps) analysis.MaxFps = fps;
                    if (analysis.MinFps == 0 || fps < analysis.MinFps) analysis.MinFps = fps;
                    
                    if (dimensions.Width > analysis.MaxWidth) analysis.MaxWidth = (int)dimensions.Width;
                    if (dimensions.Height > analysis.MaxHeight) analysis.MaxHeight = (int)dimensions.Height;
                }
            }

            return analysis;
        }

        private static async Task UnifyFrameRates(List<MagickImageCollection> gifCollections, 
                                                 GifConcatenationSettings settings, 
                                                 GifPropertyAnalysis analysis)
        {
            int targetFps = DetermineTargetFps(settings, analysis);

            for (int i = 0; i < gifCollections.Count; i++)
            {
                var collection = gifCollections[i];
                var currentFps = GetGifFrameRate(collection);

                if (currentFps != targetFps)
                {
                    await ResampleGifFrameRate(collection, targetFps);
                }
            }
        }

        private static int DetermineTargetFps(GifConcatenationSettings settings, GifPropertyAnalysis analysis)
        {
            switch (settings.FpsMode)
            {
                case FpsUnificationMode.AutoHighest:
                    return analysis.MaxFps;

                case FpsUnificationMode.UseReference:
                    if (settings.ReferenceFpsGifIndex >= 0 && settings.ReferenceFpsGifIndex < analysis.FrameRates.Count)
                        return analysis.FrameRates[settings.ReferenceFpsGifIndex];
                    return analysis.MaxFps;

                case FpsUnificationMode.Custom:
                    return settings.CustomFps;

                default:
                    return 30;
            }
        }

        private static async Task ResampleGifFrameRate(MagickImageCollection collection, int targetFps)
        {
            // Calculate new frame delay based on target FPS
            int newDelay = Math.Max(1, 100 / targetFps); // Delay in 1/100ths of a second

            await Task.Run(() =>
            {
                foreach (var frame in collection)
                {
                    frame.AnimationDelay = (uint)newDelay;
                }
            });
        }

        private static int GetGifFrameRate(MagickImageCollection collection)
        {
            if (collection.Count == 0) return 30;

            var avgDelay = collection.Average(frame => frame.AnimationDelay);
            if (avgDelay <= 0) return 30;

            return Math.Max(1, (int)Math.Round(100.0 / avgDelay));
        }

        private static void UnifyDimensions(List<MagickImageCollection> gifCollections, GifPropertyAnalysis analysis)
        {
            var targetSize = new MagickGeometry((uint)analysis.MaxWidth, (uint)analysis.MaxHeight);

            foreach (var collection in gifCollections)
            {
                if (collection[0].Width != targetSize.Width || collection[0].Height != targetSize.Height)
                {
                    foreach (var frame in collection)
                    {
                        frame.Resize(targetSize);
                    }
                }
            }
        }

        private static MagickImage BuildUnifiedPalette(List<MagickImageCollection> gifCollections, 
                                                      GifConcatenationSettings settings)
        {
            switch (settings.PaletteMode)
            {
                case PaletteUnificationMode.UseReference:
                    if (settings.ReferencePaletteGifIndex >= 0 && 
                        settings.ReferencePaletteGifIndex < gifCollections.Count)
                    {
                        return BuildSharedPalette(gifCollections.ToArray(), 
                                                settings.UseFasterPalette, 
                                                settings.ReferencePaletteGifIndex);
                    }
                    goto case PaletteUnificationMode.AutoMerge;

                case PaletteUnificationMode.AutoMerge:
                default:
                    return BuildSharedPalette(gifCollections.ToArray(), settings.UseFasterPalette);
            }
        }

        private static async Task ApplyUnifiedPalette(List<MagickImageCollection> gifCollections, 
                                                     MagickImage palette, 
                                                     bool useFastPalette)
        {
            var mapSettings = new QuantizeSettings
            {
                Colors = 256,
                ColorSpace = ColorSpace.RGB,
                DitherMethod = useFastPalette ? DitherMethod.No : DitherMethod.FloydSteinberg
            };

            foreach (var collection in gifCollections)
            {
                await Task.Run(() =>
                {
                    collection.Quantize(mapSettings);
                });
            }
        }

        private static MagickImageCollection ConcatenateGifCollections(List<MagickImageCollection> gifCollections)
        {
            var result = new MagickImageCollection();

            foreach (var collection in gifCollections)
            {
                foreach (var frame in collection)
                {
                    result.Add(frame.Clone());
                }
            }

            // Optimize the result
            result.Optimize();
            
            return result;
        }

        private static async Task<MagickImageCollection> ConcatenateGifCollectionsWithTransitions(
            List<MagickImageCollection> gifCollections, 
            GifConcatenationSettings settings,
            int fps,
            IProgress<(int current, int total, string status)> progress = null)
        {
            var result = new MagickImageCollection();

            if (gifCollections == null || gifCollections.Count == 0)
                return result;
                
            // Determine the target dimensions for all frames
            int maxWidth = gifCollections.Max(c => c.Count > 0 ? (int)c[0].Width : 0);
            int maxHeight = gifCollections.Max(c => c.Count > 0 ? (int)c[0].Height : 0);
            var targetGeometry = new MagickGeometry((uint)maxWidth, (uint)maxHeight) { IgnoreAspectRatio = false };

            try
            {
                for (int i = 0; i < gifCollections.Count; i++)
                {
                    var collection = gifCollections[i];
                    
                    // Add all frames from current GIF, ensuring consistent dimensions
                    foreach (var frame in collection)
                    {
                        var clonedFrame = frame.Clone();
                        
                        // Ensure consistent dimensions
                        if (clonedFrame.Width != maxWidth || clonedFrame.Height != maxHeight)
                        {
                            // Create canvas with target size and center the frame
                            var canvas = new MagickImage(MagickColors.Transparent, (uint)maxWidth, (uint)maxHeight);
                            
                            // Calculate center position
                            int x = (maxWidth - (int)clonedFrame.Width) / 2;
                            int y = (maxHeight - (int)clonedFrame.Height) / 2;
                            
                            // Composite the frame onto the canvas
                            canvas.Composite(clonedFrame, x, y, CompositeOperator.Over);
                            canvas.AnimationDelay = clonedFrame.AnimationDelay;
                            canvas.GifDisposeMethod = clonedFrame.GifDisposeMethod;
                            
                            clonedFrame.Dispose();
                            result.Add(canvas);
                        }
                        else
                        {
                            result.Add(clonedFrame);
                        }
                    }

                    // Generate transition to next GIF (if not the last one)
                    if (i < gifCollections.Count - 1 && settings.Transition != TransitionType.None)
                    {
                        var currentCollection = gifCollections[i];
                        var nextCollection = gifCollections[i + 1];

                        var transitionFrames = await Task.Run(() => TransitionGenerator.GenerateTransition(
                            currentCollection,
                            nextCollection,
                            settings.Transition,
                            settings.TransitionDuration,
                            fps,
                            progress));

                        // Add transition frames (they should already have correct dimensions)
                        foreach (var transitionFrame in transitionFrames)
                        {
                            var clonedTransition = transitionFrame.Clone();
                            
                            // Double-check dimensions for transition frames
                            if (clonedTransition.Width != maxWidth || clonedTransition.Height != maxHeight)
                            {
                                var canvas = new MagickImage(MagickColors.Transparent, (uint)maxWidth, (uint)maxHeight);
                                int x = (maxWidth - (int)clonedTransition.Width) / 2;
                                int y = (maxHeight - (int)clonedTransition.Height) / 2;
                                canvas.Composite(clonedTransition, x, y, CompositeOperator.Over);
                                canvas.AnimationDelay = clonedTransition.AnimationDelay;
                                canvas.GifDisposeMethod = clonedTransition.GifDisposeMethod;
                                
                                clonedTransition.Dispose();
                                result.Add(canvas);
                            }
                            else
                            {
                                result.Add(clonedTransition);
                            }
                        }

                        // Cleanup transition frames
                        transitionFrames.Dispose();
                    }
                }

                // Optimize the result - now all frames should have consistent dimensions
                try
                {
                    result.Optimize();
                }
                catch (Exception ex)
                {
                    // If optimization fails, continue without it
                    progress?.Report((1, 1, $"Warning: Frame optimization failed: {ex.Message}"));
                }
                
                return result;
            }
            catch
            {
                result.Dispose();
                throw;
            }
        }

        #endregion


    }

    // Helper class for analyzing GIF properties
    public class GifPropertyAnalysis
    {
        public List<int> FrameRates { get; set; } = new List<int>();
        public List<dynamic> Dimensions { get; set; } = new List<dynamic>();
        public int MaxFps { get; set; } = 0;
        public int MinFps { get; set; } = 0;
        public int MaxWidth { get; set; } = 0;
        public int MaxHeight { get; set; } = 0;
    }
}
