using System;
using System.Collections.Generic;
using System.Linq;
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
        /// <returns>Collection of transition frames</returns>
        public static MagickImageCollection GenerateTransition(
            MagickImageCollection fromCollection,
            MagickImageCollection toCollection,
            TransitionType transitionType,
            float durationSeconds,
            int fps)
        {
            if (fromCollection == null || toCollection == null || fromCollection.Count == 0 || toCollection.Count == 0)
                return new MagickImageCollection();

            if (transitionType == TransitionType.None || durationSeconds <= 0)
                return new MagickImageCollection();

            // Calculate number of transition frames
            int transitionFrames = Math.Max(1, (int)(durationSeconds * fps));
            
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
                // Generate transition frames based on type
                switch (transitionType)
                {
                    case TransitionType.Fade:
                        GenerateFadeTransition(normalizedFromFrame, normalizedToFrame, transitionFrames, transitionCollection, fps);
                        break;
                    case TransitionType.SlideLeft:
                        GenerateSlideTransition(normalizedFromFrame, normalizedToFrame, transitionFrames, transitionCollection, fps, SlideDirection.Left);
                        break;
                    case TransitionType.SlideRight:
                        GenerateSlideTransition(normalizedFromFrame, normalizedToFrame, transitionFrames, transitionCollection, fps, SlideDirection.Right);
                        break;
                    case TransitionType.SlideUp:
                        GenerateSlideTransition(normalizedFromFrame, normalizedToFrame, transitionFrames, transitionCollection, fps, SlideDirection.Up);
                        break;
                    case TransitionType.SlideDown:
                        GenerateSlideTransition(normalizedFromFrame, normalizedToFrame, transitionFrames, transitionCollection, fps, SlideDirection.Down);
                        break;
                    case TransitionType.ZoomIn:
                        GenerateZoomTransition(normalizedFromFrame, normalizedToFrame, transitionFrames, transitionCollection, fps, true);
                        break;
                    case TransitionType.ZoomOut:
                        GenerateZoomTransition(normalizedFromFrame, normalizedToFrame, transitionFrames, transitionCollection, fps, false);
                        break;
                    case TransitionType.Dissolve:
                        GenerateDissolveTransition(normalizedFromFrame, normalizedToFrame, transitionFrames, transitionCollection, fps);
                        break;
                    case TransitionType.CrossFade:
                        GenerateCrossFadeTransition(normalizedFromFrame, normalizedToFrame, transitionFrames, transitionCollection, fps);
                        break;
                    default:
                        // No transition - return empty collection
                        break;
                }

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
            int fps)
        {
            uint frameDelay = (uint)(100 / fps); // Delay in 1/100ths of a second

            for (int i = 0; i < frames; i++)
            {
                double progress = (double)i / (frames - 1);
                double fromOpacity = 1.0 - progress;
                double toOpacity = progress;

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
            SlideDirection direction)
        {
            uint frameDelay = (uint)(100 / fps);
            int width = (int)fromFrame.Width;
            int height = (int)fromFrame.Height;

            for (int i = 0; i < frames; i++)
            {
                double progress = (double)i / (frames - 1);
                
                using var transitionFrame = new MagickImage(MagickColors.Transparent, fromFrame.Width, fromFrame.Height);

                int fromX = 0, fromY = 0, toX = 0, toY = 0;

                switch (direction)
                {
                    case SlideDirection.Left:
                        fromX = (int)(-progress * width);
                        toX = (int)((1.0 - progress) * width);
                        break;
                    case SlideDirection.Right:
                        fromX = (int)(progress * width);
                        toX = (int)(-(1.0 - progress) * width);
                        break;
                    case SlideDirection.Up:
                        fromY = (int)(-progress * height);
                        toY = (int)((1.0 - progress) * height);
                        break;
                    case SlideDirection.Down:
                        fromY = (int)(progress * height);
                        toY = (int)(-(1.0 - progress) * height);
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
            bool zoomIn)
        {
            uint frameDelay = (uint)(100 / fps);

            for (int i = 0; i < frames; i++)
            {
                double progress = (double)i / (frames - 1);
                
                using var transitionFrame = new MagickImage(MagickColors.Transparent, fromFrame.Width, fromFrame.Height);

                // Calculate zoom parameters
                double scale = zoomIn ? (1.0 + progress) : (2.0 - progress);
                double opacity = 1.0 - progress;

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
                toFrameCopy.Evaluate(Channels.Alpha, EvaluateOperator.Multiply, progress);
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
            int fps)
        {
            uint frameDelay = (uint)(100 / fps);
            var random = new Random();

            // Create a dissolve pattern
            var dissolvePattern = new bool[(int)fromFrame.Width, (int)fromFrame.Height];
            var totalPixels = (int)(fromFrame.Width * fromFrame.Height);

            for (int i = 0; i < frames; i++)
            {
                double progress = (double)i / (frames - 1);
                int pixelsToReveal = (int)(progress * totalPixels);

                using var transitionFrame = fromFrame.Clone();

                // Create a mask for the dissolve effect
                using var mask = new MagickImage(MagickColors.Black, fromFrame.Width, fromFrame.Height);
                
                var revealedPixels = 0;
                while (revealedPixels < pixelsToReveal)
                {
                    int x = random.Next((int)fromFrame.Width);
                    int y = random.Next((int)fromFrame.Height);
                    
                    if (!dissolvePattern[x, y])
                    {
                        dissolvePattern[x, y] = true;
                        using var pixel = new MagickImage(MagickColors.White, 1, 1);
                        mask.Composite(pixel, x, y, CompositeOperator.Over);
                        revealedPixels++;
                    }
                }

                // Apply mask to reveal parts of the target frame
                using var maskedToFrame = toFrame.Clone();
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
            int fps)
        {
            // Cross fade is similar to fade but with more sophisticated blending
            uint frameDelay = (uint)(100 / fps);

            for (int i = 0; i < frames; i++)
            {
                double progress = (double)i / (frames - 1);
                
                using var transitionFrame = new MagickImage(MagickColors.Transparent, fromFrame.Width, fromFrame.Height);

                // Use more sophisticated blending curve for cross fade
                double easedProgress = EaseInOutCubic(progress);
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