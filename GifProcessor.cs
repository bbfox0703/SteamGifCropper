using FFMpegCore;
using FFMpegCore.Exceptions;
using FFMpegCore.Pipes;
using ImageMagick;
using ImageMagick.Drawing;
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
            catch
            {
                // Ignore and use default
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

        private static void UpdateFrameProgress(GifToolMainForm mainForm, int currentFrame, int totalFrames)
        {
            if (totalFrames <= 0) return;

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

                        mainForm.Enabled = false;
                        mainForm.pBarTaskStatus.Minimum = 0;
                        mainForm.pBarTaskStatus.Maximum = 100;
                        SetProgressBar(mainForm.pBarTaskStatus, 0, 100);
                        SetStatusText(mainForm, SteamGifCropper.Properties.Resources.Status_Processing);

                        var ranges = GetCropRanges(canvasWidth);
                        
                        await SplitGif(inputFilePath, mainForm, ranges, (int)canvasHeight);
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

        private static async Task SplitGif(string inputFilePath, GifToolMainForm mainForm, (int Start, int End)[] ranges, int canvasHeight)
        {
            SetStatusText(mainForm, SteamGifCropper.Properties.Resources.Status_CoalescingFrames);
            using var collection = new MagickImageCollection(inputFilePath);
            collection.Coalesce();
            Application.DoEvents(); // Allow UI to respond after coalesce operation
            int newHeight = canvasHeight + HeightExtension;

            var (recalculatedDelays, ticksPerSecond) = RecalculateGifDelays(collection);
            collection[0].AnimationTicksPerSecond = ticksPerSecond;

            int totalFrames = collection.Count * ranges.Length;
            int currentFrame = 0;

            for (int i = 0; i < ranges.Length; i++)
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

                        var newImage = new MagickImage(MagickColors.Transparent, (uint)copyWidth, (uint)newHeight);

                        var cropGeometry = new MagickGeometry(ranges[i].Start, 0, (uint)copyWidth, (uint)canvasHeight);
                        using (var croppedFrame = frame.Clone())
                        {
                            croppedFrame.Crop(cropGeometry);
                            croppedFrame.ResetPage();
                            newImage.Composite(croppedFrame, 0, 0, CompositeOperator.Over);
                        }

                        newImage.AnimationDelay = (uint)recalculatedDelays[frameIndex];
                        newImage.AnimationTicksPerSecond = ticksPerSecond;
                        newImage.GifDisposeMethod = GifDisposeMethod.Background;

                        partCollection.Add(newImage);

                        currentFrame++;
                        UpdateFrameProgress(mainForm, currentFrame, totalFrames);
                    }

                    string outputFile = $"{Path.GetFileNameWithoutExtension(inputFilePath)}_Part{i + 1}.gif";
                    string outputDir = Path.GetDirectoryName(inputFilePath);
                    string outputPath = Path.Combine(outputDir, outputFile);

                        partCollection.Optimize();
                        Application.DoEvents(); // Allow UI to respond after optimize operation
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

                        mainForm.pBarTaskStatus.Visible = true;
                        //mainForm.pBarTaskStatus.Value = 0;

                        partCollection.Write(outputPath);
                        Application.DoEvents(); // Allow UI to respond after write operation

                        if (mainForm.chkGifsicle.Checked)
                        {
                            SetStatusText(mainForm, SteamGifCropper.Properties.Resources.Status_GifsicleOptimizing);
                            var options = new GifsicleWrapper.GifsicleOptions
                            {
                                Colors = (int)mainForm.numUpDownPaletteSicle.Value,
                                Lossy = (int)mainForm.numUpDownLossy.Value,
                                OptimizeLevel = (int)mainForm.numUpDownOptimize.Value,
                                Dither = mainForm.DitherMethod
                            };

                            var progress = new Progress<int>(p =>
                            {
                                SetProgressBar(mainForm.pBarTaskStatus, p, 100);
                                SetStatusText(mainForm, $"{SteamGifCropper.Properties.Resources.Status_GifsicleOptimizing} ({p}%)");
                            });

                            await GifsicleWrapper.OptimizeGif(outputPath, outputPath, options, progress);
                        }
                        else
                        {
                            SetProgressBar(mainForm.pBarTaskStatus, 100, 100);
                            SetStatusText(mainForm, $"Saving part {i + 1} complete");
                        }

                        ModifyGifFile(outputPath, canvasHeight);
                }
            }
        }

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
                    dialog.chkGIFMergeFasterPaletteProcess.Checked,
                    dialog.PaletteSourceIndex);
            }
        }

        public static async Task MergeAndSplitFiveGifs(GifToolMainForm mainForm, List<string> gifFiles, bool useFasterPalette, int paletteSourceIndex)
        {
            if (gifFiles == null || gifFiles.Count != 5)
            {
                return; // Invalid input
            }

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

            try
            {
                mainForm.pBarTaskStatus.Minimum = 0;
                mainForm.pBarTaskStatus.Maximum = 100;
                SetProgressBar(mainForm.pBarTaskStatus, 0, 100);
                SetStatusText(mainForm, SteamGifCropper.Properties.Resources.Status_ValidatingProcessing);

                // Step 2: Load and validate all GIF files
                var collections = LoadAndValidateGifs(gifFiles.ToArray(), mainForm);
                if (collections == null) return;

                SetProgressBar(mainForm.pBarTaskStatus, 20, 100);

                // Step 3: Resize GIFs to specific widths (153, 153, 154, 153, 153)
                var resizedCollections = ResizeGifsToSpecificWidths(collections, mainForm);
                SetProgressBar(mainForm.pBarTaskStatus, 40, 100);

                // Step 4: Synchronize to shortest duration
                var syncedCollections = SynchronizeToShortestDuration(resizedCollections, mainForm);
                SetProgressBar(mainForm.pBarTaskStatus, 60, 100);

                // Step 5: Merge horizontally to create 766px wide GIF
                string firstGifPath = gifFiles[0];
                string mergedFileName = $"{Path.GetFileNameWithoutExtension(firstGifPath)}_merged.gif";
                string outputDir = Path.GetDirectoryName(firstGifPath);
                string mergedFilePath = Path.Combine(outputDir, mergedFileName);

                MergeGifsHorizontally(syncedCollections, mergedFilePath, mainForm, useFasterPalette,
                    ResourceLimits.Memory, ResourceLimits.Disk, paletteSourceIndex);
                SetProgressBar(mainForm.pBarTaskStatus, 80, 100);

                // Step 6: Apply existing split functionality
                var ranges = GetCropRanges(SupportedWidth1); // Use 766px ranges
                int adjustedHeight = (int)syncedCollections[0][0].Height + HeightExtension;
                await SplitGif(mergedFilePath, mainForm, ranges, adjustedHeight);

                // Note: mergedFilePath is kept as the intermediate merged file

                SetProgressBar(mainForm.pBarTaskStatus, 100, 100);
                SetStatusText(mainForm, SteamGifCropper.Properties.Resources.MergeFiveGif_Success);
                WindowsThemeManager.ShowThemeAwareMessageBox(mainForm,
                    SteamGifCropper.Properties.Resources.Message_FiveGifMergeComplete,
                    SteamGifCropper.Properties.Resources.Title_Success,
                    MessageBoxButtons.OK, MessageBoxIcon.Information);

                // Dispose collections
                foreach (var collection in syncedCollections)
                {
                    collection.Dispose();
                }
            }
            catch (Exception ex)
            {
                SetStatusText(mainForm, SteamGifCropper.Properties.Resources.MergeFiveGif_Error);
                WindowsThemeManager.ShowThemeAwareMessageBox(mainForm,
                    string.Format(SteamGifCropper.Properties.Resources.Error_Processing, ex.Message),
                    SteamGifCropper.Properties.Resources.Title_Error, MessageBoxButtons.OK, MessageBoxIcon.Error);
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
            mainForm.pBarTaskStatus.Minimum = 0;
            mainForm.pBarTaskStatus.Maximum = maxFrames;
            SetProgressBar(mainForm.pBarTaskStatus, 0, mainForm.pBarTaskStatus.Maximum);

            // Create enumerators for each collection to fetch frames on demand
            var enumerators = collections.Select(c => c.GetEnumerator()).ToArray();

            try
            {
                using var stream = File.Open(outputPath, FileMode.Create);
                var defines = new GifWriteDefines { RepeatCount = 0, WriteMode = GifWriteMode.Gif };

                for (int frameIndex = 0; frameIndex < maxFrames; frameIndex++)
                {
                    using var canvas = new MagickImage(MagickColors.Transparent, 766, (uint)maxHeight);

                    // X positions for each GIF: 0, 153, 306, 460, 613
                    int[] xPositions = { 0, 153, 306, 460, 613 };

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

                    canvas.Write(stream, defines);
                    defines.WriteMode = GifWriteMode.Frame;
                    UpdateFrameProgressByFrame(mainForm, frameIndex + 1, maxFrames);
                }
            }
            finally
            {
                foreach (var e in enumerators)
                {
                    e.Dispose();
                }
                palette.Dispose();

                // Reset progress bar after merging completes
                SetProgressBar(mainForm.pBarTaskStatus, 0, mainForm.pBarTaskStatus.Maximum);
                mainForm.pBarTaskStatus.Maximum = 100;
            }
        }

        public static void SplitGif(string inputFilePath, string outputDirectory)
        {
            using var collection = new MagickImageCollection(inputFilePath);
            collection.Coalesce();
            Application.DoEvents(); // Allow UI to respond after coalesce operation

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
                Application.DoEvents(); // Allow UI to respond after optimize operation
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
            try
            {
                using (var collection = new MagickImageCollection(inputFilePath))
                {
                    collection.Coalesce();
                Application.DoEvents(); // Allow UI to respond after coalesce operation

                    int totalFrames = collection.Count;
                    int currentFrame = 0;

                    if (mainForm != null)
                    {
                        mainForm.pBarTaskStatus.Minimum = 0;
                        mainForm.pBarTaskStatus.Maximum = totalFrames;
                        SetProgressBar(mainForm.pBarTaskStatus, 0, mainForm.pBarTaskStatus.Maximum);
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

                    collection.Optimize();
                    Application.DoEvents(); // Allow UI to respond after optimize operation
                    collection.Write(outputFilePath);
                    Application.DoEvents(); // Allow UI to respond after write operation
                }
            }
            finally
            {
                if (mainForm != null)
                {
                    SetProgressBar(mainForm.pBarTaskStatus, 0, mainForm.pBarTaskStatus.Maximum);
                    mainForm.pBarTaskStatus.Maximum = 100;
                    SetStatusText(mainForm, SteamGifCropper.Properties.Resources.Status_Idle);
                }
            }
        }

        public static void ResizeGifTo766(GifToolMainForm mainForm)
        {
            using (var openFileDialog = new OpenFileDialog
            {
                Filter = SteamGifCropper.Properties.Resources.FileDialog_GifFilter,
                Title = SteamGifCropper.Properties.Resources.FileDialog_SelectGifResize
            })
            {
                if (openFileDialog.ShowDialog() != DialogResult.OK) return;

                string inputFilePath = openFileDialog.FileName;
                string outputFilePath = GenerateOutputPath(inputFilePath, "_766px");

                mainForm.Enabled = false;
                try
                {
                    SetProgressBar(mainForm.pBarTaskStatus, 0, mainForm.pBarTaskStatus.Maximum);
                    mainForm.pBarTaskStatus.Maximum = 100;
                    mainForm.pBarTaskStatus.Visible = true;
                    SetStatusText(mainForm, SteamGifCropper.Properties.Resources.Status_Loading);

                    ResizeGifTo766(inputFilePath, outputFilePath, mainForm);

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
                    SetProgressBar(mainForm.pBarTaskStatus, 0, mainForm.pBarTaskStatus.Maximum);
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
                SetStatusText(mainForm, SteamGifCropper.Properties.Resources.Message_AnalyzingGifs);
                await Task.Delay(1); // Allow UI update
                var widths = new List<int>();
                int minFrameCount = int.MaxValue;
                double shortestDuration = double.MaxValue;

                // Load all GIFs and analyze properties
                foreach (string gifPath in gifPaths)
                {
                    SetStatusText(mainForm, gifPath);
                    await Task.Delay(1); // Allow UI update
                    var collection = new MagickImageCollection(gifPath);
                    collection.Coalesce();
                    Application.DoEvents(); // Allow UI to respond after coalesce operation
                    collections.Add(collection);

                    int width = (int)collection[0].Width;
                    widths.Add(width);

                    // Calculate total duration in seconds accounting for ticks-per-second
                    double totalDuration = collection.Sum(frame => (double)frame.AnimationDelay / frame.AnimationTicksPerSecond);
                    if (totalDuration < shortestDuration)
                    {
                        shortestDuration = totalDuration;
                    }

                    minFrameCount = Math.Min(minFrameCount, collection.Count);
                }

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

                mainForm.Enabled = false;
                SetStatusText(mainForm, SteamGifCropper.Properties.Resources.Message_MergingGifs);
                await Task.Delay(1); // Allow UI update
                // Build shared palette from first frames
                var palette = BuildSharedPalette(collections, useFastPalette, primaryGifIndex);
                Application.DoEvents(); // Allow UI to respond after palette building

                var mergedCollection = new MagickImageCollection();

                try
                {
                    for (int frameIndex = 0; frameIndex < targetFrameCount; frameIndex++)
                    {
                        // Update progress more frequently for better user feedback
                        if (frameIndex % 2 == 0 || frameIndex == targetFrameCount - 1)
                        {
                            SetStatusText(mainForm, $"{SteamGifCropper.Properties.Resources.Message_MergingGifs} ({frameIndex + 1}/{targetFrameCount})");
                            await Task.Delay(1); // Allow UI update
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
                    await Task.Delay(1); // Allow UI update
                    var mapSettings = new QuantizeSettings
                    {
                        Colors = 256,
                        ColorSpace = ColorSpace.RGB,
                        DitherMethod = useFastPalette ? DitherMethod.No : DitherMethod.FloydSteinberg
                    };

                    //mergedCollection.Count;
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
                            await Task.Delay(1); // Allow UI update
                            Application.DoEvents(); // Allow UI to respond during palette mapping
                        }
                    }

                    // Apply LZW compression
                    SetStatusText(mainForm, "Processing LZW compression...");
                    await Task.Delay(1); // Allow UI update
                    foreach (var frame in mergedCollection)
                    {
                        frame.Format = MagickFormat.Gif;
                        frame.Settings.SetDefine(MagickFormat.Gif, "optimize-transparency", "true");
                    }

                    // Save the merged GIF
                    SetStatusText(mainForm, SteamGifCropper.Properties.Resources.Status_Saving);
                    await Task.Delay(1); // Allow UI update                    
                    mergedCollection.Write(outputPath);
                    Application.DoEvents(); // Allow UI to respond after write operation

                    string successMessage = string.Format(SteamGifCropper.Properties.Resources.Message_GifMergeComplete, outputPath);
                    WindowsThemeManager.ShowThemeAwareMessageBox(mainForm, successMessage, SteamGifCropper.Properties.Resources.Title_Success, MessageBoxButtons.OK, MessageBoxIcon.Information);

                    SetStatusText(mainForm, SteamGifCropper.Properties.Resources.Status_Done);
                }
                finally
                {
                    palette.Dispose();
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
            var token = CreateFfmpegCancellationToken();
            
            try
            {
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
                    .CancellableThrough(token)
                    .ProcessAsynchronously();
            }
        }

        private static (bool isAvailable, string ffmpegPath, string version, string error) GetFFmpegDiagnostics()
        {
            try
            {
                // First, try to find FFmpeg in PATH
                string ffmpegPath = "ffmpeg"; // Will use PATH lookup
                
                var process = new Process
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
                    try { process.Kill(); } catch { }
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
                        SetProgressBar(mainForm.pBarTaskStatus, 25, mainForm.pBarTaskStatus.Maximum);
                        
                        // Get target framerate from main form
                        int fallbackFramerate = (int)mainForm.numUpDownFramerate.Value;
                        
                        using (var collection = new MagickImageCollection(inputFilePath))
                        {
                            SetProgressBar(mainForm.pBarTaskStatus, 50, mainForm.pBarTaskStatus.Maximum);
                            await Task.Delay(1);
                            
                            // Reverse the frame order
                            collection.Reverse();
                            
                            SetProgressBar(mainForm.pBarTaskStatus, 75, mainForm.pBarTaskStatus.Maximum);
                            await Task.Delay(1);
                            
                            // Apply framerate setting to all frames
                            uint frameDelay = (uint)(100.0 / fallbackFramerate); // Convert fps to delay (in 1/100th seconds)
                            foreach (var frame in collection)
                            {
                                frame.AnimationDelay = frameDelay;
                            }
                            
                            SetProgressBar(mainForm.pBarTaskStatus, 90, mainForm.pBarTaskStatus.Maximum);
                            await Task.Delay(1);
                            
                            collection.Write(outputFilePath);
                            Application.DoEvents(); // Allow UI to respond after write operation
                            
                            SetProgressBar(mainForm.pBarTaskStatus, 100, mainForm.pBarTaskStatus.Maximum);
                            SetStatusText(mainForm, SteamGifCropper.Properties.Resources.Status_GifReversed ?? "GIF reversed successfully!");
                            WindowsThemeManager.ShowThemeAwareMessageBox(
                                mainForm,
                                (SteamGifCropper.Properties.Resources.Message_GifReversedSuccess ?? "GIF reversed successfully!") + $"\n{outputFilePath}",
                                SteamGifCropper.Properties.Resources.Title_Success ?? "Success",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Information);
                        }
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
            double step = 0;
            if (durationSeconds > 0)
            {
                frames = Math.Max(1, durationSeconds * targetFramerate);
                frames = Math.Min(frames, distance);
                step = (double)distance / frames;
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
                    int offset = (int)Math.Round(step * i);
                    offsetX = signX * offset;
                    offsetY = signY * offset;
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
            double step = 0;
            if (durationSeconds > 0)
            {
                frames = Math.Max(1, durationSeconds * targetFramerate);
                frames = Math.Min(frames, distance);
                step = (double)distance / frames;
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
                    int offset = (int)Math.Round(step * i);
                    offsetX = signX * offset;
                    offsetY = signY * offset;
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
            Application.DoEvents(); // Allow UI to respond after write operation

            if (mainForm != null)
            {
                mainForm.Invoke((Action)(() =>
                {
                    SetStatusText(mainForm, Resources.Status_Done);                }));
            }
        }

        public static async Task ScrollStaticImage(GifToolMainForm mainForm)
        {
            using var dialog = new ScrollStaticImageDialog();
            if (dialog.ShowDialog() != DialogResult.OK)
                return;

            string inputPath = dialog.InputFilePath;
            string outputPath = dialog.OutputFilePath;
            ScrollDirection direction = dialog.Direction;
            int step = dialog.StepPixels;
            int duration = dialog.DurationSeconds;
            int moveCount = dialog.MoveCount;
            bool fullCycle = dialog.FullCycle;
            int targetFramerate = (int)mainForm.numUpDownFramerate.Value;

            mainForm.Enabled = false;
            try
            {
                mainForm.pBarTaskStatus.Visible = true;
                SetProgressBar(mainForm.pBarTaskStatus, 0, mainForm.pBarTaskStatus.Maximum);
                SetStatusText(mainForm, SteamGifCropper.Properties.Resources.Status_Processing);

                await Task.Run(() => ScrollStaticImage(inputPath, outputPath, direction, step, duration, fullCycle, moveCount, targetFramerate, mainForm));

                if (mainForm.chkGifsicle.Checked)
                {
                    var options = new GifsicleWrapper.GifsicleOptions
                    {
                        Colors = (int)mainForm.numUpDownPaletteSicle.Value,
                        Lossy = (int)mainForm.numUpDownLossy.Value,
                        OptimizeLevel = (int)mainForm.numUpDownOptimize.Value,
                        Dither = mainForm.DitherMethod
                    };
                    SetStatusText(mainForm, SteamGifCropper.Properties.Resources.Status_GifsicleOptimizing);
                    await GifsicleWrapper.OptimizeGif(outputPath, outputPath, options);
                }

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

        public static async Task OverlayGif(GifToolMainForm mainForm)
        {
            using var dialog = new OverlayGifDialog();
            if (dialog.ShowDialog(mainForm) != DialogResult.OK)
                return;

            string basePath = dialog.BaseGifPath;
            string overlayPath = dialog.OverlayGifPath;
            int offsetX = dialog.OverlayX;
            int offsetY = dialog.OverlayY;
            bool resampleBase = dialog.ResampleBaseFrames;
            string outputPath = null;

            try
            {
                SetStatusText(mainForm, SteamGifCropper.Properties.Resources.Status_Loading);
                mainForm.pBarTaskStatus.Minimum = 0;
                mainForm.pBarTaskStatus.Maximum = 100;
                SetProgressBar(mainForm.pBarTaskStatus, 0, mainForm.pBarTaskStatus.Maximum);
                using var baseCollection = new MagickImageCollection(basePath);
                using var overlayCollection = new MagickImageCollection(overlayPath);
                using var resultCollection = new MagickImageCollection();

                uint baseWidth = baseCollection[0].Width;
                uint baseHeight = baseCollection[0].Height;

                baseCollection.Coalesce();
                overlayCollection.Coalesce();

                SetStatusText(mainForm, SteamGifCropper.Properties.Resources.Status_Overlaying);
                if (resampleBase)
                {
                    var resampledBaseFrames = ResampleBaseFrames(baseCollection, overlayCollection);
                    int overlayCount = overlayCollection.Count;

                    for (int i = 0; i < overlayCount; i++)
                    {
                        using var baseFrame = resampledBaseFrames[i];
                        using var overlayFrame = overlayCollection[i].Clone();

                        int width = (int)Math.Min(overlayFrame.Width, baseWidth - (uint)offsetX);
                        int height = (int)Math.Min(overlayFrame.Height, baseHeight - (uint)offsetY);
                        if (width <= 0 || height <= 0)
                            continue;

                        overlayFrame.Crop(new MagickGeometry(0, 0, (uint)width, (uint)height));
                        overlayFrame.Page = new MagickGeometry(0, 0, overlayFrame.Width, overlayFrame.Height);

                        baseFrame.Composite(overlayFrame, offsetX, offsetY, CompositeOperator.Over);
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

                        int width = (int)Math.Min(overlayFrame.Width, baseWidth - (uint)offsetX);
                        int height = (int)Math.Min(overlayFrame.Height, baseHeight - (uint)offsetY);
                        if (width <= 0 || height <= 0)
                        {
                            baseElapsed += (int)baseCollection[i].AnimationDelay;
                            continue;
                        }

                        overlayFrame.Crop(new MagickGeometry(0, 0, (uint)width, (uint)height));
                        overlayFrame.Page = new MagickGeometry(0, 0, overlayFrame.Width, overlayFrame.Height);

                        baseFrame.Composite(overlayFrame, offsetX, offsetY, CompositeOperator.Over);
                        baseFrame.GifDisposeMethod = GifDisposeMethod.Background;

                        resultCollection.Add(baseFrame.Clone());

                        baseElapsed += (int)baseCollection[i].AnimationDelay;
                        UpdateFrameProgress(mainForm, i + 1, baseCount);
                    }
                }

                resultCollection.Quantize();
                resultCollection.Optimize();

                using var saveDialog = new SaveFileDialog
                {
                    Filter = SteamGifCropper.Properties.Resources.FileDialog_GifFilter,
                    FileName = Path.GetFileNameWithoutExtension(basePath) + "_overlay.gif",
                    Title = "Save GIF",
                };
                if (saveDialog.ShowDialog() != DialogResult.OK)
                    return;

                outputPath = saveDialog.FileName;

                SetStatusText(mainForm, SteamGifCropper.Properties.Resources.Status_Saving);                resultCollection.Write(outputPath);
            }
            catch (Exception ex)
            {
                SetStatusText(mainForm, SteamGifCropper.Properties.Resources.Status_Error);
                WindowsThemeManager.ShowThemeAwareMessageBox(mainForm,
                    $"Error: {ex.Message}",
                    SteamGifCropper.Properties.Resources.Title_Error,
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            finally
            {
                SetProgressBar(mainForm.pBarTaskStatus, 0, mainForm.pBarTaskStatus.Maximum);
                SetStatusText(mainForm, SteamGifCropper.Properties.Resources.Status_Idle);
            }

            if (!string.IsNullOrEmpty(outputPath) && mainForm.chkGifsicle.Checked)
            {
                SetStatusText(mainForm, SteamGifCropper.Properties.Resources.Status_GifsicleOptimizing);                var options = new GifsicleWrapper.GifsicleOptions
                {
                    Colors = (int)mainForm.numUpDownPaletteSicle.Value,
                    Lossy = (int)mainForm.numUpDownLossy.Value,
                    OptimizeLevel = (int)mainForm.numUpDownOptimize.Value,
                    Dither = mainForm.DitherMethod,
                };

                await GifsicleWrapper.OptimizeGif(outputPath, outputPath, options);
            }

            SetStatusText(mainForm, SteamGifCropper.Properties.Resources.Status_Done);
            WindowsThemeManager.ShowThemeAwareMessageBox(mainForm,
                SteamGifCropper.Properties.Resources.Message_OverlayComplete,
                SteamGifCropper.Properties.Resources.Title_Success,
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }


    }
}
