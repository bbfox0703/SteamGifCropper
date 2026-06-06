using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using ImageMagick;

namespace GifProcessorApp
{
    public static class TransitionGenerator
    {
        /// <summary>
        /// Generate transition frames between two GIF collections
        /// </summary>
        /// <param name="fromCollection">Source GIF collection</param>
        /// <param name="toCollection">Target GIF collection</param>
        /// <param name="transitionType">Type of transition effect</param>
        /// <param name="durationSeconds">Duration of transition in seconds</param>
        /// <param name="fps">Frames per second for the transition</param>
        /// <param name="progress">Progress reporter for operation status</param>
        /// <returns>Collection of transition frames</returns>
        public static MagickImageCollection GenerateTransition(
            MagickImageCollection fromCollection,
            MagickImageCollection toCollection,
            TransitionType transitionType,
            float durationSeconds,
            int fps,
            IProgress<(int current, int total, string status)> progress = null,
            CancellationToken cancellationToken = default)
        {
            if (fromCollection == null || toCollection == null || fromCollection.Count == 0 || toCollection.Count == 0)
                return new MagickImageCollection();

            if (transitionType == TransitionType.None || durationSeconds <= 0)
                return new MagickImageCollection();
                
            // Check for cancellation
            cancellationToken.ThrowIfCancellationRequested();

            // Calculate number of transition frames
            int transitionFrames = Math.Max(1, (int)(durationSeconds * fps));
            
            progress?.Report((0, transitionFrames, $"Initializing {transitionType} transition..."));
            
            // Get the last frame from source and first frame from target
            var fromFrame = fromCollection[fromCollection.Count - 1] as MagickImage;
            var toFrame = toCollection[0] as MagickImage;

            if (fromFrame == null || toFrame == null)
                return new MagickImageCollection();

            // Ensure both frames have the same dimensions
            var maxWidth = Math.Max((int)fromFrame.Width, (int)toFrame.Width);
            var maxHeight = Math.Max((int)fromFrame.Height, (int)toFrame.Height);

            // Resize frames to match dimensions if needed
            var normalizedFromFrame = NormalizeFrameSize(fromFrame, maxWidth, maxHeight);
            var normalizedToFrame = NormalizeFrameSize(toFrame, maxWidth, maxHeight);

            var transitionCollection = new MagickImageCollection();

            try
            {
                // Check available memory before proceeding
                GC.Collect();
                GC.WaitForPendingFinalizers();
                
                // Generate transition frames based on type
                switch (transitionType)
                {
                    case TransitionType.Fade:
                        GenerateFadeTransition(normalizedFromFrame, normalizedToFrame, transitionFrames, transitionCollection, fps, progress, cancellationToken);
                        break;
                    case TransitionType.SlideLeft:
                        GenerateSlideTransition(normalizedFromFrame, normalizedToFrame, transitionFrames, transitionCollection, fps, SlideDirection.Left, progress, cancellationToken);
                        break;
                    case TransitionType.SlideRight:
                        GenerateSlideTransition(normalizedFromFrame, normalizedToFrame, transitionFrames, transitionCollection, fps, SlideDirection.Right, progress, cancellationToken);
                        break;
                    case TransitionType.SlideUp:
                        GenerateSlideTransition(normalizedFromFrame, normalizedToFrame, transitionFrames, transitionCollection, fps, SlideDirection.Up, progress, cancellationToken);
                        break;
                    case TransitionType.SlideDown:
                        GenerateSlideTransition(normalizedFromFrame, normalizedToFrame, transitionFrames, transitionCollection, fps, SlideDirection.Down, progress, cancellationToken);
                        break;
                    case TransitionType.ZoomIn:
                        GenerateZoomTransition(normalizedFromFrame, normalizedToFrame, transitionFrames, transitionCollection, fps, true, progress, cancellationToken);
                        break;
                    case TransitionType.ZoomOut:
                        GenerateZoomTransition(normalizedFromFrame, normalizedToFrame, transitionFrames, transitionCollection, fps, false, progress, cancellationToken);
                        break;
                    case TransitionType.Dissolve:
                        GenerateDissolveTransition(normalizedFromFrame, normalizedToFrame, transitionFrames, transitionCollection, fps, progress, cancellationToken);
                        break;
                    case TransitionType.CrossFade:
                        GenerateCrossFadeTransition(normalizedFromFrame, normalizedToFrame, transitionFrames, transitionCollection, fps, progress, cancellationToken);
                        break;
                    default:
                        // No transition - return empty collection
                        break;
                }

                progress?.Report((transitionFrames, transitionFrames, $"Completed {transitionType} transition"));
                
                // Optimize the collection to reduce memory usage
                transitionCollection.Optimize();
                
                return transitionCollection;
            }
            finally
            {
                // Cleanup normalized frames if they were created
                if (normalizedFromFrame != fromFrame)
                    normalizedFromFrame.Dispose();
                if (normalizedToFrame != toFrame)
                    normalizedToFrame.Dispose();
            }
        }

        private static MagickImage NormalizeFrameSize(MagickImage sourceFrame, int targetWidth, int targetHeight)
        {
            if (sourceFrame.Width == targetWidth && sourceFrame.Height == targetHeight)
            {
                return sourceFrame; // No need to resize
            }

            var normalizedFrame = sourceFrame.Clone();
            
            // Create a canvas with the target size and transparent background
            using var canvas = new MagickImage(MagickColors.Transparent, (uint)targetWidth, (uint)targetHeight);
            
            // Calculate center position for the frame
            int x = (targetWidth - (int)normalizedFrame.Width) / 2;
            int y = (targetHeight - (int)normalizedFrame.Height) / 2;
            
            // Composite the frame onto the canvas
            canvas.Composite(normalizedFrame, x, y, CompositeOperator.Over);
            
            normalizedFrame.Dispose();
            return canvas.Clone() as MagickImage;
        }

        #region Fade Transition

        private static void GenerateFadeTransition(
            MagickImage fromFrame, 
            MagickImage toFrame, 
            int frames, 
            MagickImageCollection collection,
            int fps,
            IProgress<(int current, int total, string status)> progress = null,
            CancellationToken cancellationToken = default)
        {
            uint frameDelay = (uint)Math.Max(1, 100 / Math.Max(1, fps)); // Delay in 1/100ths of a second

            for (int i = 0; i < frames; i++)
            {
                double fadeProgress = frames == 1 ? 1.0 : (double)i / (frames - 1);
                double fromOpacity = 1.0 - fadeProgress;
                double toOpacity = fadeProgress;
                
                progress?.Report((i + 1, frames, $"Generating fade frame {i + 1}/{frames}"));
                
                // Allow UI to update periodically and check for cancellation
                if (i % 5 == 0)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    System.Threading.Thread.Yield();
                }

                using var transitionFrame = new MagickImage(MagickColors.Transparent, fromFrame.Width, fromFrame.Height);

                // Apply from frame with decreasing opacity
                using var fromFrameCopy = fromFrame.Clone();
                fromFrameCopy.Evaluate(Channels.Alpha, EvaluateOperator.Multiply, fromOpacity);
                transitionFrame.Composite(fromFrameCopy, CompositeOperator.Over);

                // Apply to frame with increasing opacity  
                using var toFrameCopy = toFrame.Clone();
                toFrameCopy.Evaluate(Channels.Alpha, EvaluateOperator.Multiply, toOpacity);
                transitionFrame.Composite(toFrameCopy, CompositeOperator.Over);

                transitionFrame.AnimationDelay = frameDelay;
                transitionFrame.GifDisposeMethod = GifDisposeMethod.Background;
                collection.Add(transitionFrame.Clone());
            }
        }

        #endregion

        #region Slide Transition

        private enum SlideDirection { Left, Right, Up, Down }

        private static void GenerateSlideTransition(
            MagickImage fromFrame,
            MagickImage toFrame,
            int frames,
            MagickImageCollection collection,
            int fps,
            SlideDirection direction,
            IProgress<(int current, int total, string status)> progress = null,
            CancellationToken cancellationToken = default)
        {
            uint frameDelay = (uint)Math.Max(1, 100 / Math.Max(1, fps));
            int width = (int)fromFrame.Width;
            int height = (int)fromFrame.Height;

            for (int i = 0; i < frames; i++)
            {
                double slideProgress = frames == 1 ? 1.0 : (double)i / (frames - 1);
                
                progress?.Report((i + 1, frames, $"Generating slide {direction.ToString().ToLower()} frame {i + 1}/{frames}"));
                
                // Allow UI to update periodically and check for cancellation
                if (i % 5 == 0)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    System.Threading.Thread.Yield();
                }
                
                using var transitionFrame = new MagickImage(MagickColors.Transparent, fromFrame.Width, fromFrame.Height);

                int fromX = 0, fromY = 0, toX = 0, toY = 0;

                switch (direction)
                {
                    case SlideDirection.Left:
                        fromX = (int)(-slideProgress * width);
                        toX = (int)((1.0 - slideProgress) * width);
                        break;
                    case SlideDirection.Right:
                        fromX = (int)(slideProgress * width);
                        toX = (int)(-(1.0 - slideProgress) * width);
                        break;
                    case SlideDirection.Up:
                        fromY = (int)(-slideProgress * height);
                        toY = (int)((1.0 - slideProgress) * height);
                        break;
                    case SlideDirection.Down:
                        fromY = (int)(slideProgress * height);
                        toY = (int)(-(1.0 - slideProgress) * height);
                        break;
                }

                // Composite from frame
                transitionFrame.Composite(fromFrame, fromX, fromY, CompositeOperator.Over);
                
                // Composite to frame
                transitionFrame.Composite(toFrame, toX, toY, CompositeOperator.Over);

                transitionFrame.AnimationDelay = frameDelay;
                transitionFrame.GifDisposeMethod = GifDisposeMethod.Background;
                collection.Add(transitionFrame.Clone());
            }
        }

        #endregion

        #region Zoom Transition

        private static void GenerateZoomTransition(
            MagickImage fromFrame,
            MagickImage toFrame,
            int frames,
            MagickImageCollection collection,
            int fps,
            bool zoomIn,
            IProgress<(int current, int total, string status)> progress = null,
            CancellationToken cancellationToken = default)
        {
            uint frameDelay = (uint)Math.Max(1, 100 / Math.Max(1, fps));

            for (int i = 0; i < frames; i++)
            {
                double zoomProgress = frames == 1 ? 1.0 : (double)i / (frames - 1);
                
                progress?.Report((i + 1, frames, $"Generating zoom {(zoomIn ? "in" : "out")} frame {i + 1}/{frames}"));
                
                // Allow UI to update periodically and check for cancellation
                if (i % 5 == 0)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    System.Threading.Thread.Yield();
                }
                
                using var transitionFrame = new MagickImage(MagickColors.Transparent, fromFrame.Width, fromFrame.Height);

                // Calculate zoom parameters
                double scale = zoomIn ? (1.0 + zoomProgress) : (2.0 - zoomProgress);
                double opacity = 1.0 - zoomProgress;

                // Apply zoom effect to from frame
                using var scaledFromFrame = fromFrame.Clone();
                scaledFromFrame.Resize(new MagickGeometry((uint)(fromFrame.Width * scale), (uint)(fromFrame.Height * scale)));
                scaledFromFrame.Evaluate(Channels.Alpha, EvaluateOperator.Multiply, opacity);

                // Center the scaled frame
                int x = ((int)fromFrame.Width - (int)scaledFromFrame.Width) / 2;
                int y = ((int)fromFrame.Height - (int)scaledFromFrame.Height) / 2;
                
                transitionFrame.Composite(scaledFromFrame, x, y, CompositeOperator.Over);

                // Fade in the target frame
                using var toFrameCopy = toFrame.Clone();
                toFrameCopy.Evaluate(Channels.Alpha, EvaluateOperator.Multiply, zoomProgress);
                transitionFrame.Composite(toFrameCopy, CompositeOperator.Over);

                transitionFrame.AnimationDelay = frameDelay;
                transitionFrame.GifDisposeMethod = GifDisposeMethod.Background;
                collection.Add(transitionFrame.Clone());
            }
        }

        #endregion

        #region Dissolve Transition

        private static void GenerateDissolveTransition(
            MagickImage fromFrame,
            MagickImage toFrame,
            int frames,
            MagickImageCollection collection,
            int fps,
            IProgress<(int current, int total, string status)> progress = null,
            CancellationToken cancellationToken = default)
        {
            uint frameDelay = (uint)Math.Max(1, 100 / Math.Max(1, fps));

            int width = (int)fromFrame.Width;
            int height = (int)fromFrame.Height;
            int totalPixels = width * height;

            // For large images, limit dissolve complexity to preserve memory
            if (totalPixels > 500000) // If image is larger than ~700x700
            {
                frames = Math.Min(frames, 10); // Limit frames to reduce memory usage
            }

            // Assign every pixel a stable random threshold once. A pixel is revealed on frame f
            // as soon as its threshold <= cutoff(f). Because the threshold is fixed across frames,
            // the reveal is monotonic with no per-frame accumulation. The previous implementation
            // re-counted "revealed" pixels from zero each frame while sharing a cumulative pattern,
            // so once that pattern filled up the inner loop could spin forever (app hang). It also
            // composited a 1x1 image per pixel; this builds the whole mask with one SetPixels call.
            var random = new Random();
            var thresholds = new byte[totalPixels];
            random.NextBytes(thresholds);

            for (int i = 0; i < frames; i++)
            {
                double dissolveProgress = frames == 1 ? 1.0 : (double)i / (frames - 1);
                int cutoff = (int)Math.Round(dissolveProgress * 255.0);

                progress?.Report((i + 1, frames, $"Generating dissolve frame {i + 1}/{frames}"));

                // Allow UI to update periodically and check for cancellation
                if (i % 3 == 0)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    System.Threading.Thread.Yield();
                }

                using var transitionFrame = fromFrame.Clone();
                using var maskedToFrame = toFrame.Clone();

                // Build a grayscale reveal mask: white (255) = revealed, black (0) = hidden.
                using var mask = new MagickImage(MagickColors.Black, (uint)width, (uint)height);
                int channels = (int)mask.ChannelCount;
                var maskPixels = new byte[totalPixels * channels];
                for (int p = 0; p < totalPixels; p++)
                {
                    byte value = thresholds[p] <= cutoff ? (byte)255 : (byte)0;
                    int baseIndex = p * channels;
                    for (int c = 0; c < channels; c++)
                    {
                        maskPixels[baseIndex + c] = value;
                    }
                }

                using (var pixels = mask.GetPixels())
                {
                    pixels.SetPixels(maskPixels);
                }

                // Copy the mask intensity into the target frame's alpha, then lay it over the source.
                maskedToFrame.Composite(mask, CompositeOperator.CopyAlpha);
                transitionFrame.Composite(maskedToFrame, CompositeOperator.Over);

                transitionFrame.AnimationDelay = frameDelay;
                transitionFrame.GifDisposeMethod = GifDisposeMethod.Background;
                collection.Add(transitionFrame.Clone());
            }
        }

        #endregion

        #region Cross Fade Transition

        private static void GenerateCrossFadeTransition(
            MagickImage fromFrame,
            MagickImage toFrame,
            int frames,
            MagickImageCollection collection,
            int fps,
            IProgress<(int current, int total, string status)> progress = null,
            CancellationToken cancellationToken = default)
        {
            // Cross fade is similar to fade but with more sophisticated blending
            uint frameDelay = (uint)Math.Max(1, 100 / Math.Max(1, fps));

            for (int i = 0; i < frames; i++)
            {
                double crossFadeProgress = frames == 1 ? 1.0 : (double)i / (frames - 1);
                
                progress?.Report((i + 1, frames, $"Generating crossfade frame {i + 1}/{frames}"));
                
                // Allow UI to update periodically and check for cancellation
                if (i % 5 == 0)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    System.Threading.Thread.Yield();
                }
                
                using var transitionFrame = new MagickImage(MagickColors.Transparent, fromFrame.Width, fromFrame.Height);

                // Use more sophisticated blending curve for cross fade
                double easedProgress = EaseInOutCubic(crossFadeProgress);
                double fromOpacity = 1.0 - easedProgress;
                double toOpacity = easedProgress;

                // Blend frames
                using var fromFrameCopy = fromFrame.Clone();
                fromFrameCopy.Evaluate(Channels.Alpha, EvaluateOperator.Multiply, fromOpacity);
                transitionFrame.Composite(fromFrameCopy, CompositeOperator.Over);

                using var toFrameCopy = toFrame.Clone();
                toFrameCopy.Evaluate(Channels.Alpha, EvaluateOperator.Multiply, toOpacity);
                transitionFrame.Composite(toFrameCopy, CompositeOperator.Over);

                transitionFrame.AnimationDelay = frameDelay;
                transitionFrame.GifDisposeMethod = GifDisposeMethod.Background;
                collection.Add(transitionFrame.Clone());
            }
        }

        private static double EaseInOutCubic(double t)
        {
            return t < 0.5 ? 4 * t * t * t : 1 - Math.Pow(-2 * t + 2, 3) / 2;
        }

        #endregion
    }
}