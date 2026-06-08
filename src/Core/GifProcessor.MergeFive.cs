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
                    dialog.SelectedFilePaths);
            }
        }

        public static async Task MergeAndSplitFiveGifs(GifToolMainForm mainForm, List<string> gifFiles)
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

                    MergeGifsHorizontally(syncedCollections, mergedFilePath, mainForm,
                        ResourceLimits.Memory, ResourceLimits.Disk);
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

        private static MagickImage BuildSharedPalette(IEnumerable<MagickImageCollection> collections, int primaryGifIndex = 0)
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
                        DitherMethod = DitherMethod.FloydSteinberg
                    };

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
                        DitherMethod = DitherMethod.FloydSteinberg
                    };

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
        /// <param name="memoryLimitBytes">Maximum memory usage in <c>bytes</c>.</param>
        /// <param name="diskLimitBytes">Maximum temporary disk usage in <c>bytes</c>.</param>
        private static void MergeGifsHorizontally(
            MagickImageCollection[] collections,
            string outputPath,
            GifToolMainForm mainForm,
            ulong memoryLimitBytes,
            ulong diskLimitBytes)
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

            // Prepare the shared quantize settings once (applied to the whole merged collection
            // after compositing — see the Quantize call below).
            var mapSettings = new QuantizeSettings
            {
                Colors = 256,
                ColorSpace = ColorSpace.RGB,
                DitherMethod = DitherMethod.FloydSteinberg
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
                        SetStatusText(mainForm, string.Format("Merging 5 GIFs - compositing frame {0}/{1}", frameIndex + 1, maxFrames));
                    }

                    // Collection takes ownership of the canvas; disposed with `output`.
                    output.Add(canvas);
                    UpdateFrameProgressByFrame(mainForm, frameIndex + 1, maxFrames);
                }

                // Build ONE optimal 256-colour palette from ALL merged frames and apply it, so the
                // five different source palettes fuse into a single shared table without colour
                // distortion (same approach as OverlayGif / MergeMultipleGifs).
                SetStatusText(mainForm, SteamGifCropper.Properties.Resources.Status_MappingSharedPalette);
                output.Quantize(mapSettings);

                var defines = new GifWriteDefines { RepeatCount = 0 };
                output.Write(outputPath, defines);
            }
            finally
            {
                foreach (var e in enumerators)
                {
                    e.Dispose();
                }

                // Reset progress bar after merging completes
                SetProgressBar(mainForm.pBarTaskStatus, 0, 100);
            }
        }

    }
}
