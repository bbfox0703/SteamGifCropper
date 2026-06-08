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
        public static async Task MergeMultipleGifs(List<string> gifPaths, string outputPath, GifToolMainForm mainForm)
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

            // Steam-ready GIF parts end with the 0x21 trailer byte instead of the standard 0x3B,
            // which makes ImageMagick fail to decode them. Flip any such source back to 0x3B before
            // loading, remember which files we touched, and restore the 0x21 trailer afterwards
            // (whether the merge succeeds or fails) so the user's source files are left untouched.
            List<string> steamTailFiles;
            try
            {
                steamTailFiles = FlipSteamTailToStandard(gifPaths);
            }
            catch (Exception ex)
            {
                // A trailer byte could not be rewritten (file read-only / locked). Anything already
                // flipped in this pass was rolled back, so abort cleanly per the spec.
                WindowsThemeManager.ShowThemeAwareMessageBox(mainForm,
                    string.Format(SteamGifCropper.Properties.Resources.MergeDialog_TailByteFlipError, ex.Message),
                    SteamGifCropper.Properties.Resources.Title_MergeGifError,
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                SetStatusText(mainForm, SteamGifCropper.Properties.Resources.Status_Error);
                return;
            }

            var collections = new List<MagickImageCollection>();
            bool mergeFaulted = false;
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

                // Merge holds every source frame AND the full merged result in the cache at once (and
                // Quantize runs over the whole result), so warn before a large job. Estimate = all source
                // pixels + result pixels (4 bytes/px). ResourceLimits still caps + spills to disk; this is
                // just a heads-up before a slow/heavy run.
                double srcBytes = collections.Sum(c => (double)c.Count * c[0].Width * c[0].Height * 4.0);
                double resBytes = (double)targetFrameCount * totalWidth * maxHeight * 4.0;
                if (!ConfirmLargeMemory(mainForm, (srcBytes + resBytes) / (1024.0 * 1024.0),
                        (uint)totalWidth, (uint)maxHeight, targetFrameCount))
                {
                    SetStatusText(mainForm, SteamGifCropper.Properties.Resources.Status_Idle);
                    return; // outer finally still disposes collections + restores the 0x21 trailer
                }

                var mergedCollection = new MagickImageCollection();

                try
                {
                    // All compositing / palette remap / LZW / write is CPU-heavy → background thread.
                    await Task.Run(() =>
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

                        // Every source frame is composited into the merged canvases now; free the source
                        // collections before the memory-heavy Quantize so we don't hold sources + result
                        // at the same time. (The outer finally disposes again — Dispose is idempotent.)
                        foreach (var c in collections) c.Dispose();

                        // Build ONE optimal 256-colour palette from ALL merged frames and apply it.
                        // Quantizing the whole assembled collection (the same approach OverlayGif
                        // uses) fuses the different source palettes into a single shared table
                        // WITHOUT colour distortion. The earlier code derived the palette from a
                        // single source frame, which collapsed it to a handful of colours and
                        // desaturated the entire merged result.
                        SetStatusText(mainForm, SteamGifCropper.Properties.Resources.Status_MappingSharedPalette);
                        SetProgressBar(mainForm.pBarTaskStatus, 50, 100);
                        var mapSettings = new QuantizeSettings
                        {
                            Colors = 256,
                            ColorSpace = ColorSpace.RGB,
                            DitherMethod = DitherMethod.FloydSteinberg
                        };
                        mergedCollection.Quantize(mapSettings);
                        SetProgressBar(mainForm.pBarTaskStatus, 90, 100);

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
                mergeFaulted = true;
                string errorMessage = $"Error merging GIF files: {ex.Message}";
                WindowsThemeManager.ShowThemeAwareMessageBox(mainForm, errorMessage, SteamGifCropper.Properties.Resources.Title_MergeGifError, MessageBoxButtons.OK, MessageBoxIcon.Error);
                SetStatusText(mainForm, SteamGifCropper.Properties.Resources.Status_Error);
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

                // Always restore the Steam trailer byte (0x3B -> 0x21) on the sources we flipped,
                // so the user's input files end up exactly as they started.
                if (steamTailFiles != null && steamTailFiles.Count > 0)
                {
                    List<string> notRestored = RestoreSteamTail(steamTailFiles);

                    // Per request: if the merge faulted, tell the user which sources were touched.
                    if (mergeFaulted)
                    {
                        WindowsThemeManager.ShowThemeAwareMessageBox(mainForm,
                            string.Format(SteamGifCropper.Properties.Resources.MergeDialog_TailFilesModifiedOnError,
                                string.Join(Environment.NewLine, steamTailFiles.Select(p => Path.GetFileName(p)))),
                            SteamGifCropper.Properties.Resources.Title_MergeGifError,
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }

                    // Surface any file we could not flip back to 0x21 (left at 0x3B).
                    if (notRestored.Count > 0)
                    {
                        WindowsThemeManager.ShowThemeAwareMessageBox(mainForm,
                            string.Format(SteamGifCropper.Properties.Resources.MergeDialog_TailRestoreWarning,
                                string.Join(Environment.NewLine, notRestored.Select(p => Path.GetFileName(p)))),
                            SteamGifCropper.Properties.Resources.Title_MergeGifError,
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
            }
        }

    }
}
