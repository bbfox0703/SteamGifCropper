#nullable disable
using System;
using System.Threading;
using ImageMagick;

namespace GifProcessorApp
{
    /// <summary>
    /// Generates the frames of a transition between two GIF clips.
    ///
    /// DYNAMIC (overlap) model: a transition consumes the last N frames of the outgoing clip and the
    /// first N frames of the incoming clip, where N = round(duration*fps). For transition frame i,
    /// the outgoing clip's frame (count-N+i) is blended with the incoming clip's frame i — so BOTH
    /// clips keep playing through the transition instead of freezing on a single boundary frame. The
    /// concatenation assembler trims those N frames from each side so they are shown once (the overlap
    /// shortens the total length by N frames per transition).
    /// </summary>
    public static class TransitionGenerator
    {
        /// <summary>
        /// Number of transition frames for the given duration/fps, clamped so it never exceeds either
        /// clip (the dynamic overlap needs N frames from each side). Returns 0 when there is no
        /// transition. Pure — the assembler uses it to compute the trim, the generator to size the loop.
        /// </summary>
        public static int GetFrameCount(float durationSeconds, int fps, int fromCount, int toCount)
        {
            if (durationSeconds <= 0f || fps <= 0 || fromCount <= 0 || toCount <= 0)
                return 0;
            int n = (int)Math.Round(durationSeconds * fps);
            if (n < 1) n = 1;
            int maxN = Math.Min(fromCount, toCount);
            if (n > maxN) n = maxN;
            return n;
        }

        public static MagickImageCollection GenerateTransition(
            MagickImageCollection fromCollection,
            MagickImageCollection toCollection,
            TransitionType transitionType,
            float durationSeconds,
            int fps,
            IProgress<(int current, int total, string status)> progress = null,
            CancellationToken cancellationToken = default)
        {
            var result = new MagickImageCollection();

            if (fromCollection == null || toCollection == null || fromCollection.Count == 0 || toCollection.Count == 0)
                return result;
            if (transitionType == TransitionType.None || durationSeconds <= 0)
                return result;

            int fromCount = fromCollection.Count;
            int toCount = toCollection.Count;
            int frames = GetFrameCount(durationSeconds, fps, fromCount, toCount);
            if (frames <= 0)
                return result;

            int width = Math.Max((int)fromCollection[fromCount - 1].Width, (int)toCollection[0].Width);
            int height = Math.Max((int)fromCollection[fromCount - 1].Height, (int)toCollection[0].Height);
            uint frameDelay = (uint)Math.Max(1, 100 / Math.Max(1, fps));

            // Per-transition state that must stay stable across frames.
            byte[] dissolveThresholds = null;
            if (transitionType == TransitionType.Dissolve)
            {
                dissolveThresholds = new byte[width * height];
                // Fixed seed so the mask is reproducible (and identical between runs of the same job).
                new Random(20240601).NextBytes(dissolveThresholds);
            }

            RippleDrop[] rippleDrops = null;
            RippleMedium rippleMedium = default;
            if (transitionType == TransitionType.Ripple)
            {
                double maxR = Math.Sqrt((width / 2.0) * (width / 2.0) + (height / 2.0) * (height / 2.0));
                rippleDrops = new[] { new RippleDrop(width / 2.0, height / 2.0, 0.0, 1.0) };
                rippleMedium = new RippleMedium
                {
                    WaveSpeed = Math.Max(1.0, maxR),     // front reaches the corner by t=1 (we feed t as seconds)
                    Wavelength = Math.Max(8.0, width / 6.0),
                    SpatialDamping = 0.003,
                    TimeDamping = 0.0,                   // no settling within the short transition
                    Strength = Math.Max(4.0, width / 80.0),
                    Threshold = 0.001
                };
            }

            double maxRadius = Math.Sqrt((width / 2.0) * (width / 2.0) + (height / 2.0) * (height / 2.0));

            for (int i = 0; i < frames; i++)
            {
                if (i % 4 == 0)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    Thread.Yield();
                }

                double t = frames == 1 ? 1.0 : (double)i / (frames - 1);
                progress?.Report((i + 1, frames, $"Generating {transitionType} frame {i + 1}/{frames}"));

                // Running frames: outgoing clip's tail and incoming clip's head both advance.
                using var fromFrame = Normalize((MagickImage)fromCollection[fromCount - frames + i], width, height);
                using var toFrame = Normalize((MagickImage)toCollection[i], width, height);

                MagickImage frame = RenderFrame(transitionType, fromFrame, toFrame, t, width, height,
                    dissolveThresholds, maxRadius, rippleDrops, rippleMedium);

                frame.AnimationDelay = frameDelay;
                frame.GifDisposeMethod = GifDisposeMethod.Background;
                result.Add(frame);
            }

            return result;
        }

        // ---- per-frame renderers -------------------------------------------------------------

        private static MagickImage RenderFrame(
            TransitionType type, MagickImage from, MagickImage to, double t, int w, int h,
            byte[] dissolveThresholds, double maxRadius, RippleDrop[] rippleDrops, RippleMedium rippleMedium)
        {
            switch (type)
            {
                case TransitionType.Fade:
                    return Blend(from, to, t, w, h);
                case TransitionType.CrossFade:
                    return Blend(from, to, EaseInOutCubic(t), w, h);

                case TransitionType.SlideLeft:
                    return Slide(from, to, t, w, h, -t * w, 0, (1 - t) * w, 0);
                case TransitionType.SlideRight:
                    return Slide(from, to, t, w, h, t * w, 0, -(1 - t) * w, 0);
                case TransitionType.SlideUp:
                    return Slide(from, to, t, w, h, 0, -t * h, 0, (1 - t) * h);
                case TransitionType.SlideDown:
                    return Slide(from, to, t, w, h, 0, t * h, 0, -(1 - t) * h);

                case TransitionType.ZoomIn:
                    return Zoom(from, to, t, w, h, true);
                case TransitionType.ZoomOut:
                    return Zoom(from, to, t, w, h, false);

                case TransitionType.Dissolve:
                {
                    int cutoff = (int)Math.Round(t * 255.0);
                    return Reveal(from, to, w, h, (x, y) => dissolveThresholds[y * w + x] <= cutoff);
                }

                case TransitionType.IrisOpen:
                {
                    double r = t * maxRadius;
                    double cx = w / 2.0, cy = h / 2.0;
                    return Reveal(from, to, w, h, (x, y) => Dist(x, y, cx, cy) <= r); // B inside the expanding circle
                }
                case TransitionType.IrisClose:
                {
                    double r = (1 - t) * maxRadius;
                    double cx = w / 2.0, cy = h / 2.0;
                    return Reveal(to, from, w, h, (x, y) => Dist(x, y, cx, cy) <= r); // A inside the shrinking circle, B outside
                }

                case TransitionType.WipeLeft:
                {
                    double edge = t * w;
                    return Reveal(from, to, w, h, (x, y) => x <= edge); // B sweeps in from the left
                }
                case TransitionType.WipeRight:
                {
                    double edge = (1 - t) * w;
                    return Reveal(from, to, w, h, (x, y) => x >= edge); // B sweeps in from the right
                }
                case TransitionType.WipeDiagonal:
                {
                    return Reveal(from, to, w, h, (x, y) => ((double)x / w + (double)y / h) * 0.5 <= t);
                }

                case TransitionType.DipToBlack:
                    return DipToBlack(from, to, t, w, h);
                case TransitionType.BlurDissolve:
                    return BlurDissolve(from, to, t, w, h);
                case TransitionType.Ripple:
                    return Ripple(from, to, t, w, h, rippleDrops, rippleMedium);

                default:
                    return Blend(from, to, t, w, h);
            }
        }

        // Cross-dissolve: from at opacity (1-amount) under to at opacity (amount).
        private static MagickImage Blend(MagickImage from, MagickImage to, double amount, int w, int h)
        {
            var frame = new MagickImage(MagickColors.Transparent, (uint)w, (uint)h);
            using (var a = (MagickImage)from.Clone())
            {
                a.Evaluate(Channels.Alpha, EvaluateOperator.Multiply, 1.0 - amount);
                frame.Composite(a, CompositeOperator.Over);
            }
            using (var b = (MagickImage)to.Clone())
            {
                b.Evaluate(Channels.Alpha, EvaluateOperator.Multiply, amount);
                frame.Composite(b, CompositeOperator.Over);
            }
            return frame;
        }

        private static MagickImage Slide(MagickImage from, MagickImage to, double t, int w, int h,
            double fromX, double fromY, double toX, double toY)
        {
            var frame = new MagickImage(MagickColors.Transparent, (uint)w, (uint)h);
            frame.Composite(from, (int)Math.Round(fromX), (int)Math.Round(fromY), CompositeOperator.Over);
            frame.Composite(to, (int)Math.Round(toX), (int)Math.Round(toY), CompositeOperator.Over);
            return frame;
        }

        private static MagickImage Zoom(MagickImage from, MagickImage to, double t, int w, int h, bool zoomIn)
        {
            var frame = new MagickImage(MagickColors.Transparent, (uint)w, (uint)h);
            double scale = zoomIn ? (1.0 + t) : (2.0 - t);
            using (var scaled = (MagickImage)from.Clone())
            {
                scaled.Resize(new MagickGeometry((uint)Math.Max(1, w * scale), (uint)Math.Max(1, h * scale)) { IgnoreAspectRatio = true });
                scaled.Evaluate(Channels.Alpha, EvaluateOperator.Multiply, 1.0 - t);
                int x = (w - (int)scaled.Width) / 2;
                int y = (h - (int)scaled.Height) / 2;
                frame.Composite(scaled, x, y, CompositeOperator.Over);
            }
            using (var b = (MagickImage)to.Clone())
            {
                b.Evaluate(Channels.Alpha, EvaluateOperator.Multiply, t);
                frame.Composite(b, CompositeOperator.Over);
            }
            return frame;
        }

        // Lay `overlay` over `baseFrame` only where predicate(x,y) is true (a hard reveal mask).
        private static MagickImage Reveal(MagickImage baseFrame, MagickImage overlay, int w, int h, Func<int, int, bool> revealed)
        {
            using var mask = new MagickImage(MagickColors.Black, (uint)w, (uint)h);
            int channels = (int)mask.ChannelCount;
            var maskPixels = new byte[w * h * channels];
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    byte value = revealed(x, y) ? (byte)255 : (byte)0;
                    int baseIndex = (y * w + x) * channels;
                    for (int c = 0; c < channels; c++)
                        maskPixels[baseIndex + c] = value;
                }
            }
            using (var pixels = mask.GetPixels())
            {
                pixels.SetPixels(maskPixels);
            }

            using var maskedOverlay = (MagickImage)overlay.Clone();
            maskedOverlay.Composite(mask, CompositeOperator.CopyAlpha);
            var frame = (MagickImage)baseFrame.Clone();
            frame.Composite(maskedOverlay, CompositeOperator.Over);
            return frame;
        }

        private static MagickImage DipToBlack(MagickImage from, MagickImage to, double t, int w, int h)
        {
            var frame = new MagickImage(MagickColors.Black, (uint)w, (uint)h);
            if (t < 0.5)
            {
                using var a = (MagickImage)from.Clone();
                a.Evaluate(Channels.Alpha, EvaluateOperator.Multiply, 1.0 - t / 0.5); // fade A out to black
                frame.Composite(a, CompositeOperator.Over);
            }
            else
            {
                using var b = (MagickImage)to.Clone();
                b.Evaluate(Channels.Alpha, EvaluateOperator.Multiply, (t - 0.5) / 0.5); // fade B in from black
                frame.Composite(b, CompositeOperator.Over);
            }
            return frame;
        }

        private static MagickImage BlurDissolve(MagickImage from, MagickImage to, double t, int w, int h)
        {
            double maxSigma = Math.Max(2.0, Math.Min(w, h) / 60.0);
            using var fromBlur = (MagickImage)from.Clone();
            fromBlur.Blur(0, t * maxSigma);          // A gets blurrier as it leaves
            using var toBlur = (MagickImage)to.Clone();
            toBlur.Blur(0, (1.0 - t) * maxSigma);    // B starts blurred and sharpens in
            return Blend(fromBlur, toBlur, t, w, h);
        }

        private static MagickImage Ripple(MagickImage from, MagickImage to, double t, int w, int h,
            RippleDrop[] drops, RippleMedium medium)
        {
            using var blended = Blend(from, to, EaseInOutCubic(t), w, h);
            // Feed t directly as the wave's elapsed seconds; the medium is tuned so the front crosses
            // the frame by t=1, giving a watery wobble that peaks mid-transition and settles by the end.
            return RippleRenderer.RenderFrame(blended, t, drops, medium);
        }

        private static MagickImage Normalize(MagickImage source, int targetWidth, int targetHeight)
        {
            if ((int)source.Width == targetWidth && (int)source.Height == targetHeight)
                return (MagickImage)source.Clone();

            var canvas = new MagickImage(MagickColors.Transparent, (uint)targetWidth, (uint)targetHeight);
            int x = (targetWidth - (int)source.Width) / 2;
            int y = (targetHeight - (int)source.Height) / 2;
            canvas.Composite(source, x, y, CompositeOperator.Over);
            return canvas;
        }

        private static double Dist(int x, int y, double cx, double cy)
        {
            double dx = x - cx, dy = y - cy;
            return Math.Sqrt(dx * dx + dy * dy);
        }

        private static double EaseInOutCubic(double t)
        {
            return t < 0.5 ? 4 * t * t * t : 1 - Math.Pow(-2 * t + 2, 3) / 2;
        }
    }
}
