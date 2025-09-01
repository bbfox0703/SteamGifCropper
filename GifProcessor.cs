using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;
using System.Configuration;
using FFMpegCore;
using FFMpegCore.Exceptions;
using FFMpegCore.Pipes;
using ImageMagick;
using SteamGifCropper.Properties;

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

        private static void UpdateProgress(ProgressBar progressBar, int current, int total)
        {
            if (progressBar == null || total <= 0) return;

            void UpdateUI() =>
                progressBar.Value = Math.Min((int)((double)current / total * 100), 100);

            if (progressBar.InvokeRequired)
            {
                progressBar.BeginInvoke((Action)UpdateUI);
            }
            else
            {
                UpdateUI();
            }
        }

        private static void UpdateStatusLabel(GifToolMainForm mainForm, string text)
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
                mainForm.pBarTaskStatus.Value = percent;
                mainForm.lblStatus.Text = $"{currentFrame}/{totalFrames} ({percent}%)";
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
                mainForm.pBarTaskStatus.Value = Math.Min(currentFrame, totalFrames);
                int percent = Math.Min((int)((double)currentFrame / totalFrames * 100), 100);
                mainForm.lblStatus.Text = $"{currentFrame}/{totalFrames} ({percent}%)";
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

        public static void StartProcessing(GifToolMainForm mainForm)
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

                        mainForm.pBarTaskStatus.Minimum = 0;
                        mainForm.pBarTaskStatus.Maximum = 100;
                        UpdateProgress(mainForm.pBarTaskStatus, 0, 100);
                        UpdateStatusLabel(mainForm, SteamGifCropper.Properties.Resources.Status_Processing);

                        var ranges = GetCropRanges(canvasWidth);
                        int targetFramerate = (int)mainForm.numUpDownFramerate.Value;
                        SplitGif(inputFilePath, mainForm, ranges, (int)canvasHeight, targetFramerate);
                        mainForm.lblStatus.Text = SteamGifCropper.Properties.Resources.Status_Done;
                        WindowsThemeManager.ShowThemeAwareMessageBox(mainForm,
                                        SteamGifCropper.Properties.Resources.Message_ProcessingComplete,
                                        SteamGifCropper.Properties.Resources.Title_Success, MessageBoxButtons.OK, MessageBoxIcon.Information);

                    }
                }
                catch (Exception ex)
                {
                    mainForm.lblStatus.Text = SteamGifCropper.Properties.Resources.Status_Error;
                    WindowsThemeManager.ShowThemeAwareMessageBox(mainForm,
                                    string.Format(SteamGifCropper.Properties.Resources.Error_Occurred, ex.Message),
                                    SteamGifCropper.Properties.Resources.Title_Error, MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                finally
                {
                    mainForm.pBarTaskStatus.Value = 0;
                    mainForm.lblStatus.Text = SteamGifCropper.Properties.Resources.Status_Idle;
                }
            }
        }
        private static void SplitGif(string inputFilePath, GifToolMainForm mainForm, (int Start, int End)[] ranges, int canvasHeight, int targetFramerate = 15)
        {
            UpdateStatusLabel(mainForm, SteamGifCropper.Properties.Resources.Status_CoalescingFrames);
            using var collection = new MagickImageCollection(inputFilePath);
            collection.Coalesce();
            int newHeight = canvasHeight + HeightExtension;
            
            int totalFrames = collection.Count * ranges.Length;
            int currentFrame = 0;

            for (int i = 0; i < ranges.Length; i++)
            {
                using (var partCollection = new MagickImageCollection())
                {
                    foreach (var frame in collection)
                    {
                        int copyWidth = ranges[i].End - ranges[i].Start + 1;

                        if (currentFrame % ProgressUpdateInterval == 0)
                        {
                            UpdateStatusLabel(mainForm, string.Format(SteamGifCropper.Properties.Resources.Status_ProcessingPart, i + 1, (currentFrame % collection.Count) + 1));
                        }

                        // Create new image with correct dimensions
                        var newImage = new MagickImage(MagickColors.Transparent, (uint)copyWidth, (uint)newHeight);

                        // Crop the frame to the specific range
                        var cropGeometry = new MagickGeometry(ranges[i].Start, 0, (uint)copyWidth, (uint)canvasHeight);
                        using (var croppedFrame = frame.Clone())
                        {
                            croppedFrame.Crop(cropGeometry);
                            croppedFrame.ResetPage();

                            // Composite the cropped frame onto the new image
                            newImage.Composite(croppedFrame, 0, 0, CompositeOperator.Over);
                        }

                        // Preserve animation timing from source frame
                        newImage.AnimationDelay = frame.AnimationDelay;
                        newImage.AnimationTicksPerSecond = frame.AnimationTicksPerSecond;
                        newImage.GifDisposeMethod = GifDisposeMethod.Background;

                        partCollection.Add(newImage);

                        currentFrame++;
                        UpdateFrameProgress(mainForm, currentFrame, totalFrames);
                    }

                        string outputFile = $"{Path.GetFileNameWithoutExtension(inputFilePath)}_Part{i + 1}.gif";
                        string outputDir = Path.GetDirectoryName(inputFilePath);
                        string outputPath = Path.Combine(outputDir, outputFile);

                        partCollection.Optimize();
                        UpdateStatusLabel(mainForm, SteamGifCropper.Properties.Resources.Status_Compressing);
                        int compressFrameCount = 0;
                        foreach (var frame in partCollection)
                        {
                            frame.Settings.SetDefine("compress", "LZW");

                            if (++compressFrameCount % 25 == 0)
                            {
                                UpdateStatusLabel(mainForm, SteamGifCropper.Properties.Resources.Status_Compressing);
                            }
                        }

                        UpdateStatusLabel(mainForm, SteamGifCropper.Properties.Resources.Status_Saving);

                        partCollection.Write(outputPath);

                        if (mainForm.chkGifsicle.Checked)
                        {
                            UpdateStatusLabel(mainForm, SteamGifCropper.Properties.Resources.Status_GifsicleOptimizing);
                            var options = new GifsicleWrapper.GifsicleOptions
                            {
                                Colors = (int)mainForm.numUpDownPaletteSicle.Value,
                                Lossy = (int)mainForm.numUpDownLossy.Value,
                                OptimizeLevel = (int)mainForm.numUpDownOptimize.Value,
                                Dither = mainForm.DitherMethod
                            };

                            GifsicleWrapper.OptimizeGif(outputPath, outputPath, options).GetAwaiter().GetResult();
                        }

                        ModifyGifFile(outputPath, canvasHeight);
                }
            }
        }

        public static void MergeAndSplitFiveGifs(GifToolMainForm mainForm)
        {
            // Step 1: Select five GIF files in order
            var gifFiles = SelectFiveOrderedGifs();
            if (gifFiles == null || gifFiles.Length != 5)
            {
                return; // User cancelled or didn't select exactly 5 files
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
                UpdateProgress(mainForm.pBarTaskStatus, 0, 100);
                UpdateStatusLabel(mainForm, SteamGifCropper.Properties.Resources.Status_ValidatingProcessing);

                // Step 2: Load and validate all GIF files
                var collections = LoadAndValidateGifs(gifFiles, mainForm);
                if (collections == null) return;

                UpdateProgress(mainForm.pBarTaskStatus, 20, 100);

                // Step 3: Resize GIFs to specific widths (153, 153, 154, 153, 153)
                var resizedCollections = ResizeGifsToSpecificWidths(collections, mainForm);
                UpdateProgress(mainForm.pBarTaskStatus, 40, 100);

                // Step 4: Synchronize to shortest duration
                var syncedCollections = SynchronizeToShortestDuration(resizedCollections, mainForm);
                UpdateProgress(mainForm.pBarTaskStatus, 60, 100);

                // Step 5: Merge horizontally to create 766px wide GIF
                bool useFastPalette = mainForm.chk5GIFMergeFasterPaletteProcess.Checked;
                string firstGifPath = gifFiles[0];
                string mergedFileName = $"{Path.GetFileNameWithoutExtension(firstGifPath)}_merged.gif";
                string outputDir = Path.GetDirectoryName(firstGifPath);
                string mergedFilePath = Path.Combine(outputDir, mergedFileName);

                MergeGifsHorizontally(syncedCollections, mergedFilePath, mainForm, useFastPalette,
                    ResourceLimits.Memory, ResourceLimits.Disk);
                UpdateProgress(mainForm.pBarTaskStatus, 80, 100);

                // Step 6: Apply existing split functionality
                var ranges = GetCropRanges(SupportedWidth1); // Use 766px ranges
                int adjustedHeight = (int)syncedCollections[0][0].Height + HeightExtension;
                int targetFramerate = (int)mainForm.numUpDownFramerate.Value;
                SplitGif(mergedFilePath, mainForm, ranges, adjustedHeight, targetFramerate);

                // Note: mergedFilePath is kept as the intermediate merged file

                UpdateProgress(mainForm.pBarTaskStatus, 100, 100);
                mainForm.lblStatus.Text = SteamGifCropper.Properties.Resources.MergeFiveGif_Success;
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
                mainForm.lblStatus.Text = SteamGifCropper.Properties.Resources.MergeFiveGif_Error;
                WindowsThemeManager.ShowThemeAwareMessageBox(mainForm,
                    string.Format(SteamGifCropper.Properties.Resources.Error_Processing, ex.Message),
                    SteamGifCropper.Properties.Resources.Title_Error, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static string[] SelectFiveOrderedGifs()
        {
            var selectedFiles = new List<string>();
            
            WindowsThemeManager.ShowThemeAwareMessageBox(null,
                SteamGifCropper.Properties.Resources.Instruction_SelectFiveGifs,
                SteamGifCropper.Properties.Resources.Title_SelectionOrder,
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            
            for (int i = 1; i <= 5; i++)
            {
                using (var openFileDialog = new OpenFileDialog
                {
                    Filter = SteamGifCropper.Properties.Resources.FileDialog_GifFilter,
                    Title = string.Format(SteamGifCropper.Properties.Resources.FileDialog_SelectGifOrder, i, i),
                    Multiselect = false
                })
                {
                    if (openFileDialog.ShowDialog() != DialogResult.OK)
                    {
                        // User cancelled
                        return null;
                    }
                    selectedFiles.Add(openFileDialog.FileName);
                }
            }
            
            return selectedFiles.ToArray();
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
                    mainForm.lblStatus.Text = string.Format(
                        SteamGifCropper.Properties.Resources.Status_ResizingGif,
                        i + 1, targetWidths[i]);
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
                            mainForm.lblStatus.Text = string.Format(
                                SteamGifCropper.Properties.Resources.Status_ResizingGifFrame,
                                i + 1, frameCount, collections[i].Count);                        }
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
            UpdateStatusLabel(mainForm, SteamGifCropper.Properties.Resources.Status_SynchronizingDurations);

            // Calculate total duration for each GIF in seconds
            var durations = new double[5];
            for (int i = 0; i < 5; i++)
            {
                durations[i] = collections[i].Sum(frame => (double)frame.AnimationDelay / frame.AnimationTicksPerSecond);
            }

            // Find shortest duration
            double shortestDuration = durations.Min();
            int shortestIndex = Array.IndexOf(durations, shortestDuration);

            UpdateStatusLabel(mainForm, string.Format(
                SteamGifCropper.Properties.Resources.Status_ShortestDuration,
                shortestDuration, shortestIndex + 1));

            // Synchronize all GIFs to shortest duration
            var syncedCollections = new MagickImageCollection[5];

            try
            {
                for (int i = 0; i < 5; i++)
                {
                    UpdateStatusLabel(mainForm, string.Format(
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
                                UpdateStatusLabel(mainForm, string.Format(
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
                                    UpdateStatusLabel(mainForm, string.Format(
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

        private static MagickImage BuildSharedPalette(IEnumerable<MagickImageCollection> collections, bool useFastPalette)
        {
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
            ulong diskLimitBytes)
        {
            UpdateStatusLabel(mainForm, SteamGifCropper.Properties.Resources.Status_MergingHorizontally);

            // Enable disk caching to limit memory usage
            MagickNET.SetTempDirectory(Path.GetTempPath());

            // Apply resource limits configured by Program.ConfigureResourceLimits
            // Values are in bytes for consistency with that configuration.
            ResourceLimits.Memory = memoryLimitBytes;
            ResourceLimits.Disk = diskLimitBytes;

            // Calculate maximum height among all resized GIFs
            int maxHeight = collections.Max(c => (int)c[0].Height);

            // Build shared palette from first frames
            var palette = BuildSharedPalette(collections, useFastPalette);

            // Prepare remap settings once
            var mapSettings = new QuantizeSettings
            {
                Colors = 256,
                ColorSpace = ColorSpace.RGB,
                DitherMethod = useFastPalette ? DitherMethod.No : DitherMethod.FloydSteinberg
            };

            int maxFrames = collections.Max(c => c.Count);

            // Create enumerators for each collection to fetch frames on demand
            var enumerators = collections.Select(c => c.GetEnumerator()).ToArray();

            try
            {
                using var stream = File.Open(outputPath, FileMode.Create);
                var defines = new GifWriteDefines { RepeatCount = 0, WriteMode = GifWriteMode.Gif };

                for (int frameIndex = 0; frameIndex < maxFrames; frameIndex++)
                {
                    // Update UI every 10 frames during merging
                    if (frameIndex % ProgressUpdateInterval == 0)
                    {
                        UpdateStatusLabel(mainForm, string.Format(
                            SteamGifCropper.Properties.Resources.Status_MergingFrame,
                            frameIndex + 1, maxFrames));
                    }

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

                    // Remap frame to shared palette before writing
                    canvas.Remap(palette, mapSettings);

                    canvas.Write(stream, defines);
                    defines.WriteMode = GifWriteMode.Frame;
                }
            }
            finally
            {
                foreach (var e in enumerators)
                {
                    e.Dispose();
                }
                palette.Dispose();
            }
        }

        public static void SplitGif(string inputFilePath, string outputDirectory, int targetFramerate = 15)
        {
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

            for (int i = 0; i < ranges.Length; i++)
            {
                using var partCollection = new MagickImageCollection();
                foreach (var frame in collection)
                {
                    int copyWidth = ranges[i].End - ranges[i].Start + 1;

                    using var newImage = new MagickImage(MagickColors.Transparent, (uint)copyWidth, (uint)newHeight);
                    var cropGeometry = new MagickGeometry(ranges[i].Start, 0, (uint)copyWidth, (uint)canvasHeight);
                    using var croppedFrame = frame.Clone();
                    croppedFrame.Crop(cropGeometry);
                    croppedFrame.ResetPage();
                    newImage.Composite(croppedFrame, 0, 0, CompositeOperator.Over);
                    newImage.AnimationDelay = frame.AnimationDelay;
                    newImage.AnimationTicksPerSecond = frame.AnimationTicksPerSecond;
                    newImage.GifDisposeMethod = GifDisposeMethod.Background;
                    partCollection.Add(newImage.Clone());
                }

                partCollection.Optimize();
                foreach (var frame in partCollection)
                {
                    frame.Settings.SetDefine("compress", "LZW");
                }

                string outputFile = Path.Combine(outputDirectory, $"{Path.GetFileNameWithoutExtension(inputFilePath)}_Part{i + 1}.gif");
                partCollection.Write(outputFile);
                ModifyGifFile(outputFile, canvasHeight);
            }
        }

        public static void SplitGifWithReducedPalette(GifToolMainForm mainForm)
        {
            // Keep the original method name for backward compatibility
            // but redirect to the new merge and split functionality
            MergeAndSplitFiveGifs(mainForm);
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

                        mainForm.lblStatus.Text = SteamGifCropper.Properties.Resources.Status_ProcessingPalette;
                        mainForm.pBarTaskStatus.Minimum = 0;
                        mainForm.pBarTaskStatus.Maximum = 100;
                        mainForm.pBarTaskStatus.Value = 0;

                        var ranges = GetCropRanges(canvasWidth);
                        int targetFramerate = (int)mainForm.numUpDownFramerate.Value;

                        var progress = new Progress<(int current, int total, string status)>(report =>
                        {
                            mainForm.Invoke((MethodInvoker)(() =>
                            {
                                if (report.total > 0)
                                {
                                    mainForm.pBarTaskStatus.Value = Math.Min(report.current * 100 / report.total, 100);
                                }
                                mainForm.lblStatus.Text = report.status;
                            }));
                        });

                        await ReducePaletteAndSplitGif(inputFilePath, ranges, (int)canvasHeight, paletteSize, targetFramerate, progress);
                        mainForm.lblStatus.Text = SteamGifCropper.Properties.Resources.Status_Done;
                        WindowsThemeManager.ShowThemeAwareMessageBox(mainForm,
                                        SteamGifCropper.Properties.Resources.Message_PaletteProcessingComplete,
                                        SteamGifCropper.Properties.Resources.Title_Success, MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
                catch (Exception ex)
                {
                    mainForm.lblStatus.Text = SteamGifCropper.Properties.Resources.Status_Error;
                    WindowsThemeManager.ShowThemeAwareMessageBox(mainForm,
                                    string.Format(SteamGifCropper.Properties.Resources.Error_Occurred, ex.Message),
                                    SteamGifCropper.Properties.Resources.Title_Error, MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                finally
                {
                    mainForm.pBarTaskStatus.Value = 0;
                    mainForm.lblStatus.Text = SteamGifCropper.Properties.Resources.Status_Idle;
                }
            }
        }

        private static async Task ReducePaletteAndSplitGif(string inputFilePath, (int Start, int End)[] ranges, int canvasHeight, int paletteSize, int targetFramerate, IProgress<(int current, int total, string status)> progress)
        {
            await Task.Run(() =>
            {
                using (var collection = new MagickImageCollection(inputFilePath))
                {
                    progress?.Report((0, 1, SteamGifCropper.Properties.Resources.Status_CoalescingFrames));
                    collection.Coalesce();

                    int newHeight = canvasHeight + HeightExtension;

                    // Calculate target frame delay in centiseconds (1/100th of a second)
                    uint targetDelay = (uint)Math.Round(100.0 / targetFramerate);

                    int totalSteps = (collection.Count * ranges.Length) + (ranges.Length * 3); // Processing + Palette reduction + LZW compression
                    int currentStep = 0;

                    for (int i = 0; i < ranges.Length; i++)
                    {
                        using (var partCollection = new MagickImageCollection())
                        {
                            foreach (var frame in collection)
                            {
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

                                    // Set animation properties to target framerate
                                    newImage.AnimationDelay = targetDelay;
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
                            currentStep++;
                            progress?.Report((currentStep, totalSteps, SteamGifCropper.Properties.Resources.Status_Compressing));

                            foreach (var frame in partCollection)
                            {
                                frame.Settings.SetDefine("compress", "LZW");
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

                    int totalFrames = collection.Count;
                    int currentFrame = 0;

                    if (mainForm != null)
                    {
                        mainForm.pBarTaskStatus.Minimum = 0;
                        mainForm.pBarTaskStatus.Maximum = totalFrames;
                        mainForm.pBarTaskStatus.Value = 0;
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
                    collection.Write(outputFilePath);
                }
            }
            finally
            {
                if (mainForm != null)
                {
                    mainForm.pBarTaskStatus.Value = 0;
                    mainForm.pBarTaskStatus.Maximum = 100;
                    mainForm.lblStatus.Text = SteamGifCropper.Properties.Resources.Status_Idle;
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

                try
                {
                    mainForm.pBarTaskStatus.Value = 0;
                    mainForm.pBarTaskStatus.Maximum = 100;
                    mainForm.pBarTaskStatus.Visible = true;
                    UpdateStatusLabel(mainForm, SteamGifCropper.Properties.Resources.Status_Loading);

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
                    mainForm.pBarTaskStatus.Value = 0;
                    mainForm.lblStatus.Text = SteamGifCropper.Properties.Resources.Status_Idle;
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

                try
                {
                    UpdateStatusLabel(mainForm, SteamGifCropper.Properties.Resources.Status_RestoringTailBytes);
                    mainForm.pBarTaskStatus.Value = 0;
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
                        UpdateProgress(mainForm.pBarTaskStatus, progress, selectedFiles.Length);
                        if (progress % ProgressUpdateInterval == 0 || progress == selectedFiles.Length)
                        {
                            UpdateStatusLabel(mainForm, string.Format(
                                SteamGifCropper.Properties.Resources.Status_ProcessingCount,
                                progress, selectedFiles.Length));
                        }
                    }

                    string resultMessage = string.Format(
                        SteamGifCropper.Properties.Resources.Message_RestorationComplete,
                        processedCount, skippedCount);

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
                    mainForm.pBarTaskStatus.Value = 0;
                    mainForm.pBarTaskStatus.Visible = false;
                    mainForm.lblStatus.Text = SteamGifCropper.Properties.Resources.Status_Ready;
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

                try
                {
                    mainForm.pBarTaskStatus.Visible = true;
                    mainForm.pBarTaskStatus.Value = 0;
                    mainForm.pBarTaskStatus.Maximum = 100;

                    foreach (string filePath in filePaths)
                    {
                        try
                        {
                            if (ProcessTailByte(filePath))
                                processedFiles++;

                            UpdateProgress(mainForm.pBarTaskStatus, processedFiles, filePaths.Length);
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
                    mainForm.pBarTaskStatus.Value = 0;
                    mainForm.pBarTaskStatus.Visible = false;
                    mainForm.lblStatus.Text = SteamGifCropper.Properties.Resources.Status_Idle;
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

        public static async Task MergeMultipleGifs(List<string> gifPaths, string outputPath, GifToolMainForm mainForm, int targetFramerate = 15, bool useFastPalette = false)
        {
            if (gifPaths == null || gifPaths.Count < 2 || gifPaths.Count > 5)
            {
                throw new ArgumentException(SteamGifCropper.Properties.Resources.Message_GifFileCount);
            }

            // Validate source files and destination path
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
                mainForm.lblStatus.Text = SteamGifCropper.Properties.Resources.Message_AnalyzingGifs;
                await Task.Delay(1); // Allow UI update
                var widths = new List<int>();
                int minFrameCount = int.MaxValue;
                double shortestDuration = double.MaxValue;

                // Load all GIFs and analyze properties
                foreach (string gifPath in gifPaths)
                {
                    var collection = new MagickImageCollection(gifPath);
                    collection.Coalesce();
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

                // Determine timing based on first GIF
                int ticksPerSecond = collections[0][0].AnimationTicksPerSecond;

                // Calculate target frame count based on shortest duration and target framerate
                int targetFrameCount = Math.Max(1, (int)(shortestDuration * targetFramerate));

                // Calculate target delay in ticks
                uint targetDelay = (uint)Math.Round((double)ticksPerSecond / targetFramerate);

                // Calculate total width
                int totalWidth = widths.Sum();
                int maxHeight = collections.Max(c => (int)c[0].Height);

                mainForm.lblStatus.Text = SteamGifCropper.Properties.Resources.Message_MergingGifs;
                await Task.Delay(1); // Allow UI update
                // Build shared palette from first frames
                var palette = BuildSharedPalette(collections, useFastPalette);

                var mergedCollection = new MagickImageCollection();

                try
                {
                    for (int frameIndex = 0; frameIndex < targetFrameCount; frameIndex++)
                    {
                        // Update progress and allow UI updates
                        if (frameIndex % 5 == 0)
                        {
                            mainForm.lblStatus.Text = $"{SteamGifCropper.Properties.Resources.Message_MergingGifs} ({frameIndex + 1}/{targetFrameCount})";
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

                        // Set animation delay and timing to maintain source speed
                        canvas.AnimationDelay = targetDelay;
                        canvas.AnimationTicksPerSecond = ticksPerSecond;
                        canvas.GifDisposeMethod = GifDisposeMethod.Background;
                        
                        mergedCollection.Add(canvas);
                    }

                    // Remap frames to shared palette
                    mainForm.lblStatus.Text = useFastPalette ?
                        SteamGifCropper.Properties.Resources.Status_MappingFastPalette :
                        SteamGifCropper.Properties.Resources.Status_MappingSharedPalette;
                    await Task.Delay(1); // Allow UI update
                    var mapSettings = new QuantizeSettings
                    {
                        Colors = 256,
                        ColorSpace = ColorSpace.RGB,
                        DitherMethod = useFastPalette ? DitherMethod.No : DitherMethod.FloydSteinberg
                    };

                    foreach (MagickImage frame in mergedCollection)
                    {
                        frame.Remap(palette, mapSettings);
                    }

                    // Apply LZW compression
                    foreach (var frame in mergedCollection)
                    {
                        frame.Format = MagickFormat.Gif;
                        frame.Settings.SetDefine(MagickFormat.Gif, "optimize-transparency", "true");
                    }

                    // Save the merged GIF
                    mainForm.lblStatus.Text = SteamGifCropper.Properties.Resources.Status_Saving;
                    await Task.Delay(1); // Allow UI update                    
                    mergedCollection.Write(outputPath);

                    string successMessage = string.Format(SteamGifCropper.Properties.Resources.Message_GifMergeComplete, outputPath);
                    WindowsThemeManager.ShowThemeAwareMessageBox(mainForm, successMessage, SteamGifCropper.Properties.Resources.Title_Success, MessageBoxButtons.OK, MessageBoxIcon.Information);

                    mainForm.lblStatus.Text = SteamGifCropper.Properties.Resources.Status_Done;
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
                mainForm.lblStatus.Text = SteamGifCropper.Properties.Resources.Status_Error;
                throw;
            }
            finally
            {
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
                var useGPU = conversionDialog.UseGPUAcceleration;
                var targetFramerate = (int)mainForm.numUpDownFramerate.Value;

                try
                {
                    mainForm.pBarTaskStatus.Visible = true;
                    mainForm.pBarTaskStatus.Value = 0;
                    if (useGPU)
                    {
                        // For short clips or small files, CPU is often faster due to GPU memory overhead
                        bool preferCpu = duration.TotalSeconds < 8;
                        if (preferCpu)
                        {
                            mainForm.lblStatus.Text = SteamGifCropper.Properties.Resources.Mp4ToGif_ShortClipCpu;
                            useGPU = false;
                        }
                        else
                        {
                            mainForm.lblStatus.Text = SteamGifCropper.Properties.Resources.Mp4ToGif_GpuCpuEncoding;
                        }
                        if (useGPU)
                        {
                            try
                            {
                            // Determine the source codec to choose the appropriate CUDA decoder
                            string decoder = null;
                            try
                            {
                                var analysis = await FFProbe.AnalyseAsync(inputPath);
                                var codec = analysis.PrimaryVideoStream?.CodecName?.ToLowerInvariant();
                                decoder = codec switch
                                {
                                    "h264" => "h264_cuvid",
                                    "hevc" => "hevc_cuvid",
                                    "h265" => "hevc_cuvid",
                                    "mpeg2video" => "mpeg2_cuvid",
                                    "mpeg4" => "mpeg4_cuvid",
                                    "vp9" => "vp9_cuvid",
                                    _ => null
                                };
                            }
                            catch
                            {
                                // If ffprobe fails, continue without specifying decoder
                                decoder = null;
                            }

                            // GPU-accelerated MP4 decoding, CPU GIF encoding
                            // Note: GIF encoding doesn't support CUDA, only decoding can be accelerated
                            var token = CreateFfmpegCancellationToken();
                            await FFMpegArguments
                                .FromFileInput(inputPath, true, input =>
                                {
                                    input.WithCustomArgument("-hwaccel cuda");
                                    if (!string.IsNullOrEmpty(decoder))
                                        input.WithCustomArgument($"-c:v {decoder}");
                                    input.WithCustomArgument("-hwaccel_output_format cuda");
                                })
                                .OutputToFile(outputPath, true, options =>
                                {
                                    options.Seek(startTime)
                                           .WithDuration(duration)
                                           // GPU-to-CPU transfer with proper format conversion
                                           .WithCustomArgument("-vf hwdownload,format=nv12,format=rgb24")
                                           .WithFramerate(targetFramerate)
                                           .WithCustomArgument("-pix_fmt rgb8");
                                    ApplyThreadLimit(options);
                                })
                                .CancellableThrough(token)
                                .ProcessAsynchronously();
                        }
                            catch (Exception gpuEx)
                            {
                                mainForm.lblStatus.Text = SteamGifCropper.Properties.Resources.Mp4ToGif_GpuDecodeFailed;
                            if (gpuEx is FFMpegException ffmpegException && !string.IsNullOrWhiteSpace(ffmpegException.FFMpegErrorOutput))
                            {
                                string logFilePath = null;
                                try
                                {
                                    string logDirectory = Path.GetDirectoryName(outputPath);
                                    if (string.IsNullOrEmpty(logDirectory) || !Directory.Exists(logDirectory))
                                        logDirectory = Path.GetTempPath();

                                    logFilePath = Path.Combine(logDirectory, "ffmpeg_gpu_error.log");
                                    File.WriteAllText(logFilePath, ffmpegException.FFMpegErrorOutput);
                                    mainForm.lblStatus.Text += string.Format(SteamGifCropper.Properties.Resources.Mp4ToGif_FFmpegLog, logFilePath);
                                }
                                catch
                                {
                                    string truncated = ffmpegException.FFMpegErrorOutput.Length > 200
                                        ? ffmpegException.FFMpegErrorOutput.Substring(0, 200) + "..."
                                        : ffmpegException.FFMpegErrorOutput;
                                    mainForm.lblStatus.Text += string.Format(SteamGifCropper.Properties.Resources.Mp4ToGif_FFmpegOutput, truncated);
                                }
                            }

                                // GPU failed, fallback to optimized CPU processing
                                await ProcessWithOptimizedCpu(inputPath, outputPath, startTime, duration, targetFramerate);
                            }
                        }
                    }
                    
                    if (!useGPU)
                    {
                        mainForm.lblStatus.Text = SteamGifCropper.Properties.Resources.Mp4ToGif_Converting;                        await ProcessWithOptimizedCpu(inputPath, outputPath, startTime, duration, targetFramerate);
                    }

                    mainForm.pBarTaskStatus.Value = 100;
                    mainForm.lblStatus.Text = SteamGifCropper.Properties.Resources.Mp4ToGif_Success;
                    
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
                    else if (ex.Message.Contains("cuda") || ex.Message.Contains("nvdec"))
                    {
                        userFriendlyMessage = SteamGifCropper.Properties.Resources.Mp4ToGif_Error_GPUFailed;
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
                    mainForm.pBarTaskStatus.Value = 0;
                    mainForm.pBarTaskStatus.Visible = false;
                    mainForm.lblStatus.Text = SteamGifCropper.Properties.Resources.Status_Ready;
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
            // Optimized CPU processing using streaming to avoid loading entire files
            await using var inputStream = File.OpenRead(inputPath);
            await using var outputStream = File.Open(outputPath, FileMode.Create, FileAccess.Write);

            var token = CreateFfmpegCancellationToken();
            await FFMpegArguments
                .FromPipeInput(new StreamPipeSource(inputStream))
                .OutputToPipe(new StreamPipeSink(outputStream), options =>
                {
                    options.ForceFormat("gif")
                           .Seek(startTime)
                           .WithDuration(duration)
                           .WithFramerate(targetFramerate)
                           .WithCustomArgument("-pix_fmt rgb8")
                           .WithCustomArgument("-an");
                    ApplyThreadLimit(options);
                })
                .CancellableThrough(token)
                .ProcessAsynchronously();
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

                mainForm.lblStatus.Text = SteamGifCropper.Properties.Resources.Status_ReversingGif ?? "Reversing GIF...";
                mainForm.pBarTaskStatus.Visible = true;
                mainForm.pBarTaskStatus.Value = 0;

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

                    mainForm.pBarTaskStatus.Value = 25;
                    await Task.Delay(1);

                    // Use FFMpegCore to reverse the GIF
                    var inputAnalysis = await FFProbe.AnalyseAsync(inputFilePath);
                    var totalDuration = inputAnalysis.Duration;

                    mainForm.pBarTaskStatus.Value = 50;
                    await Task.Delay(1);
                    // Reverse GIF directly with palettegen + paletteuse using streaming to limit memory usage
                    mainForm.pBarTaskStatus.Value = 75;
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

                    mainForm.pBarTaskStatus.Value = 100;
                    await Task.Delay(1);

                    mainForm.lblStatus.Text = SteamGifCropper.Properties.Resources.Status_GifReversed ?? "GIF reversed successfully!";
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
                        mainForm.lblStatus.Text = SteamGifCropper.Properties.Resources.Status_FFmpegFallback;
                        mainForm.pBarTaskStatus.Value = 25;
                        
                        // Get target framerate from main form
                        int fallbackFramerate = (int)mainForm.numUpDownFramerate.Value;
                        
                        using (var collection = new MagickImageCollection(inputFilePath))
                        {
                            mainForm.pBarTaskStatus.Value = 50;
                            await Task.Delay(1);
                            
                            // Reverse the frame order
                            collection.Reverse();
                            
                            mainForm.pBarTaskStatus.Value = 75;
                            await Task.Delay(1);
                            
                            // Apply framerate setting to all frames
                            uint frameDelay = (uint)(100.0 / fallbackFramerate); // Convert fps to delay (in 1/100th seconds)
                            foreach (var frame in collection)
                            {
                                frame.AnimationDelay = frameDelay;
                            }
                            
                            mainForm.pBarTaskStatus.Value = 90;
                            await Task.Delay(1);
                            
                            collection.Write(outputFilePath);
                            
                            mainForm.pBarTaskStatus.Value = 100;
                            mainForm.lblStatus.Text = SteamGifCropper.Properties.Resources.Status_GifReversed ?? "GIF reversed successfully!";
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
                        mainForm.lblStatus.Text = SteamGifCropper.Properties.Resources.Status_Error ?? "Error";
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
                    mainForm.pBarTaskStatus.Visible = false;
                    mainForm.lblStatus.Text = SteamGifCropper.Properties.Resources.Status_Ready ?? "Ready";
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

            uint delay = (uint)Math.Round(100.0 / targetFramerate);

            if (File.Exists(outputFilePath))
                File.Delete(outputFilePath);

            using var collection = new MagickImageCollection();

            mainForm?.Invoke((Action)(() =>
            {
                mainForm.pBarTaskStatus.Maximum = frames;
                mainForm.pBarTaskStatus.Value = 0;
                mainForm.lblStatus.Text = string.Format(Resources.Status_ProcessingFrame, 0, frames);
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
                        mainForm.pBarTaskStatus.Value = current;
                        mainForm.lblStatus.Text = string.Format(Resources.Status_ProcessingFrame, current, frames);
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
                    mainForm.lblStatus.Text = Resources.Status_Saving;                }));
            }

            collection.Write(outputFilePath, defines);

            if (mainForm != null)
            {
                mainForm.Invoke((Action)(() =>
                {
                    mainForm.lblStatus.Text = Resources.Status_Done;                }));
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
                mainForm.pBarTaskStatus.Value = 0;
                mainForm.lblStatus.Text = SteamGifCropper.Properties.Resources.Status_Processing;

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
                    mainForm.lblStatus.Text = SteamGifCropper.Properties.Resources.Status_GifsicleOptimizing;                    await GifsicleWrapper.OptimizeGif(outputPath, outputPath, options);
                }

                mainForm.pBarTaskStatus.Value = mainForm.pBarTaskStatus.Maximum;
                mainForm.lblStatus.Text = SteamGifCropper.Properties.Resources.Status_Done;
                WindowsThemeManager.ShowThemeAwareMessageBox(mainForm,
                                SteamGifCropper.Properties.Resources.Message_ProcessingComplete,
                                SteamGifCropper.Properties.Resources.Title_Success,
                                MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (MagickResourceLimitErrorException)
            {
                mainForm.lblStatus.Text = SteamGifCropper.Properties.Resources.Status_Error;
                WindowsThemeManager.ShowThemeAwareMessageBox(mainForm,
                                SteamGifCropper.Properties.Resources.Error_CacheResourcesExhausted,
                                SteamGifCropper.Properties.Resources.Title_Error,
                                MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                mainForm.lblStatus.Text = SteamGifCropper.Properties.Resources.Status_Error;
                WindowsThemeManager.ShowThemeAwareMessageBox(mainForm,
                                string.Format(SteamGifCropper.Properties.Resources.Error_Occurred, ex.Message),
                                SteamGifCropper.Properties.Resources.Title_Error,
                                MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                mainForm.Enabled = true;
                mainForm.pBarTaskStatus.Visible = false;
                mainForm.lblStatus.Text = SteamGifCropper.Properties.Resources.Status_Ready;
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

        public static void OverlayGif(GifToolMainForm mainForm)
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
                mainForm.lblStatus.Text = SteamGifCropper.Properties.Resources.Status_Loading;
                mainForm.pBarTaskStatus.Minimum = 0;
                mainForm.pBarTaskStatus.Maximum = 100;
                mainForm.pBarTaskStatus.Value = 0;
                using var baseCollection = new MagickImageCollection(basePath);
                using var overlayCollection = new MagickImageCollection(overlayPath);
                using var resultCollection = new MagickImageCollection();

                uint baseWidth = baseCollection[0].Width;
                uint baseHeight = baseCollection[0].Height;

                baseCollection.Coalesce();
                overlayCollection.Coalesce();

                mainForm.lblStatus.Text = SteamGifCropper.Properties.Resources.Status_Overlaying;
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

                mainForm.lblStatus.Text = SteamGifCropper.Properties.Resources.Status_Saving;                resultCollection.Write(outputPath);
            }
            catch (Exception ex)
            {
                mainForm.lblStatus.Text = SteamGifCropper.Properties.Resources.Status_Error;
                WindowsThemeManager.ShowThemeAwareMessageBox(mainForm,
                    $"Error: {ex.Message}",
                    SteamGifCropper.Properties.Resources.Title_Error,
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            finally
            {
                mainForm.pBarTaskStatus.Value = 0;
                mainForm.lblStatus.Text = SteamGifCropper.Properties.Resources.Status_Idle;
            }

            if (!string.IsNullOrEmpty(outputPath) && mainForm.chkGifsicle.Checked)
            {
                mainForm.lblStatus.Text = SteamGifCropper.Properties.Resources.Status_GifsicleOptimizing;                var options = new GifsicleWrapper.GifsicleOptions
                {
                    Colors = (int)mainForm.numUpDownPaletteSicle.Value,
                    Lossy = (int)mainForm.numUpDownLossy.Value,
                    OptimizeLevel = (int)mainForm.numUpDownOptimize.Value,
                    Dither = mainForm.DitherMethod,
                };

                GifsicleWrapper.OptimizeGif(outputPath, outputPath, options).GetAwaiter().GetResult();
            }

            mainForm.lblStatus.Text = SteamGifCropper.Properties.Resources.Status_Done;
            WindowsThemeManager.ShowThemeAwareMessageBox(mainForm,
                SteamGifCropper.Properties.Resources.Message_OverlayComplete,
                SteamGifCropper.Properties.Resources.Title_Success,
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }


    }
}
