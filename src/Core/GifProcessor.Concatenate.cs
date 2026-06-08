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
                    await ApplyUnifiedPalette(gifCollections, unifiedPalette);
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
                                                settings.ReferencePaletteGifIndex);
                    }
                    goto case PaletteUnificationMode.AutoMerge;

                case PaletteUnificationMode.AutoMerge:
                default:
                    return BuildSharedPalette(gifCollections.ToArray());
            }
        }

        private static async Task ApplyUnifiedPalette(List<MagickImageCollection> gifCollections,
                                                     MagickImage palette)
        {
            var mapSettings = new QuantizeSettings
            {
                Colors = 256,
                ColorSpace = ColorSpace.RGB,
                DitherMethod = DitherMethod.FloydSteinberg
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
