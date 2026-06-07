using System;

namespace GifProcessorApp
{
    // Pure, dependency-free quicksand-flow math so it can be linked into the test project (which uses
    // a GifProcessor stub instead of the Magick-heavy real class).
    //
    // Model: the image is sliced into N horizontal bands. Each band wrap-scrolls horizontally by a
    // WHOLE number of revolutions over the flow duration, eased in/out so it starts AND ends aligned
    // with the original image — i.e. a seamless loop. The revolution count varies across the bands to
    // form a viscous shear gradient: the "fast band" (bottom / top / middle) does MaxRevolutions, the
    // opposite extreme does MinRevolutions, interpolated by a viscosity curve. The returned offset
    // feeds MagickImage.Roll(offset, 0) (or Roll(0, offset) for a future vertical variant — the math is
    // axis-agnostic, only the caller's crop/roll axis differs).
    public enum QuicksandFastBand
    {
        Bottom = 0,
        Top = 1,
        Middle = 2,
    }

    public static class QuicksandGeometry
    {
        // Ease-in-out cubic: slow start, fast middle, slow finish. Clamped to [0, 1]. Used so the flow
        // accelerates from rest and decelerates back to rest, giving a smooth (no velocity jump) loop.
        public static double EaseInOutCubic(double t)
        {
            if (t <= 0.0) return 0.0;
            if (t >= 1.0) return 1.0;
            if (t < 0.5)
            {
                return 4.0 * t * t * t;
            }
            double f = -2.0 * t + 2.0;
            return 1.0 - (f * f * f) / 2.0;
        }

        // Start offset + size (px) of band `index` when `totalLength` px is split into `layers` bands.
        // The first `remainder` bands get one extra px so the bands exactly tile the length (no gaps,
        // no overlap), mirroring GridMosaicGeometry's remainder distribution.
        public static (int Start, int Size) BandBounds(int index, int layers, int totalLength)
        {
            if (layers < 1) layers = 1;
            if (index < 0) index = 0;
            if (index >= layers) index = layers - 1;
            if (totalLength < 0) totalLength = 0;

            int baseSize = totalLength / layers;
            int remainder = totalLength % layers;
            int start = index * baseSize + Math.Min(index, remainder);
            int size = baseSize + (index < remainder ? 1 : 0);
            return (start, size);
        }

        // Normalized speed in [0, 1] of band `index` (0 = slowest, 1 = fastest) given where the fast
        // band sits. Linear toward bottom/top, triangular (peak at centre) for middle. Pure profile,
        // before the viscosity curve is applied. Band 0 is the TOP band, band (layers-1) the BOTTOM.
        public static double BandSpeed(int index, int layers, QuicksandFastBand fastBand)
        {
            if (layers <= 1) return 1.0;
            if (index < 0) index = 0;
            if (index >= layers) index = layers - 1;

            double frac = (double)index / (layers - 1); // 0 at top, 1 at bottom
            switch (fastBand)
            {
                case QuicksandFastBand.Top:
                    return 1.0 - frac;
                case QuicksandFastBand.Middle:
                    return 1.0 - Math.Abs(2.0 * frac - 1.0);
                case QuicksandFastBand.Bottom:
                default:
                    return frac;
            }
        }

        // Whole-number revolutions for band `index`. Interpolates between minRevs (slowest band) and
        // maxRevs (fastest band) using BandSpeed shaped by `viscosity` (gamma; >1 keeps the slow bands
        // slower for longer = stickier, <1 flattens the gradient). Rounded to an integer so every band
        // returns EXACTLY to its starting alignment after one cycle.
        public static int BandRevolutions(int index, int layers, int minRevs, int maxRevs,
                                          QuicksandFastBand fastBand, double viscosity)
        {
            if (minRevs < 0) minRevs = 0;
            if (maxRevs < minRevs) maxRevs = minRevs;
            if (viscosity <= 0.0) viscosity = 1.0;

            double speed = BandSpeed(index, layers, fastBand);
            double shaped = Math.Pow(speed, viscosity);
            double revs = minRevs + (maxRevs - minRevs) * shaped;
            return (int)Math.Round(revs);
        }

        // Horizontal wrap offset in [0, wrapLength) for a band at normalized time t in [0, 1].
        //   distance = revolutions * wrapLength * easeInOut(t)
        // At t = 1 the distance is a whole number of wraps, so the offset is 0 (aligned with the
        // original). flowPositive flips the apparent scroll direction (right vs left).
        public static int BandOffset(double t, int revolutions, int wrapLength, bool flowPositive)
        {
            if (wrapLength <= 0) return 0;
            if (revolutions < 0) revolutions = 0;

            double eased = EaseInOutCubic(t);
            double distance = revolutions * (double)wrapLength * eased;
            int raw = (int)Math.Round(distance);

            int offset = ((raw % wrapLength) + wrapLength) % wrapLength;
            if (!flowPositive)
            {
                offset = (wrapLength - offset) % wrapLength;
            }
            return offset;
        }
    }
}
