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
            RunMagickWithProgress(mainForm, outputCollection,
                SteamGifCropper.Properties.Resources.Status_Saving, () => outputCollection.Write(outputFilePath));

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

    }
}
