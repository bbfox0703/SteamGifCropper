using System;

namespace GifProcessorApp
{
    // Which way the water fills. Up = rises from the bottom (the normal case); the same algorithm runs in
    // all four directions. Enum order matches the dialog's direction dropdown index.
    public enum WaterDirection
    {
        Up = 0,    // fills from the bottom, surface rises
        Down = 1,  // fills from the top
        Left = 2,  // fills from the left
        Right = 3, // fills from the right
    }

    // Resolved per-canvas water parameters (WaterSettings.ToParams builds this).
    public struct WaterParams
    {
        public WaterDirection Direction;
        public double RefractionStrength; // px: underwater shimmer (light distortion) amplitude
        public double SurfaceWobble;      // px: surface wave amplitude
        public double BubbleSize;         // px: base bubble radius (count is auto from the canvas area)
        public int Layers;                // depth layers (>= 3): far = small/slow/faint, near = big/strong
        public int Seed;
        public int BubbleR, BubbleG, BubbleB; // bubble rim/highlight colour (0..255); default white (255,255,255)
    }

    // One bubble for a single frame (already known to be underwater).
    public struct WaterBubble
    {
        public double X, Y;   // centre (px)
        public double R;      // radius (px)
        public double Opacity;// 0..1 rim/fill opacity
        public double Lens;   // 0..1 lens (magnification) strength
    }

    // Pure, dependency-free math for the "fill with water + rising bubbles" effect (linked into the test
    // project; WaterRenderer does the Magick sampling). Water fills from a chosen direction between a start
    // and a full time; below the wavy, wobbling surface the source is refracted (a gentle shimmer) and seen
    // through rising bubbles that act as little lenses; above the surface nothing changes. Bubbles span
    // >= 3 depth layers (auto near/far) and exit when they reach the surface.
    public static class WaterField
    {
        // Fill fraction 0..1 at effect-elapsed time te, ramping from 0 at startSec to 1 at fullSec and
        // staying full afterwards.
        public static double FillFrac(double te, double startSec, double fullSec)
        {
            if (te <= startSec) return 0.0;
            if (fullSec <= startSec) return 1.0;
            double f = (te - startSec) / (fullSec - startSec);
            return f < 0.0 ? 0.0 : f > 1.0 ? 1.0 : f;
        }

        // Overall effect opacity at elapsed time te: 1 while filling and full, then fading 1 -> 0 over
        // fadeSeconds once the tank is full, so the whole water effect (refraction + bubbles) disappears
        // and the source returns to normal. fadeSeconds <= 0 keeps the water to the end (no fade).
        public static double EffectAlpha(double te, double fullSec, double fadeSeconds)
        {
            if (fadeSeconds <= 0.0) return 1.0;
            if (te <= fullSec) return 1.0;
            double a = 1.0 - (te - fullSec) / fadeSeconds;
            return a < 0.0 ? 0.0 : a > 1.0 ? 1.0 : a;
        }

        // Auto bubble count from the canvas area — modest, so the bubbles drift up leisurely rather than
        // swarm (the user doesn't set the count).
        public static int AutoBubbleCount(int w, int h)
        {
            int c = (int)Math.Round((double)Math.Max(1, w) * Math.Max(1, h) / 16000.0);
            if (c < 4) c = 4;
            if (c > 20) c = 20;
            return c;
        }

        public static bool Vertical(WaterDirection d) => d == WaterDirection.Up || d == WaterDirection.Down;

        // The surface coordinate along the fill axis at perpendicular position `perp` and time te. A wavy,
        // wobbling line; the wave fades out as the tank empties/fills so it is flat at the extremes.
        public static double Surface(double perp, double te, double fillFrac, int axisLen, int perpLen, WaterParams p)
        {
            // Down/Left fill from the low end (surface = fill); Up/Right fill from the high end (surface = 1-fill).
            bool fromLow = p.Direction == WaterDirection.Down || p.Direction == WaterDirection.Left;
            double baseS = fromLow ? axisLen * fillFrac : axisLen * (1.0 - fillFrac);
            double pl = Math.Max(1.0, perpLen);
            double fade = 4.0 * fillFrac * (1.0 - fillFrac); // 0 at empty/full, 1 at half
            double wave = p.SurfaceWobble * fade * Math.Sin(2.0 * Math.PI * perp / (pl / 1.5) + te * 3.0);
            return baseS + wave;
        }

        // Whether pixel (x,y) is below the water surface (i.e. submerged).
        public static bool IsUnderwater(int x, int y, double te, double fillFrac, int w, int h, WaterParams p)
        {
            if (fillFrac <= 0.0) return false;
            bool vertical = Vertical(p.Direction);
            int axisLen = vertical ? h : w;
            int perpLen = vertical ? w : h;
            double perp = vertical ? x : y;
            double axisPos = vertical ? y : x;
            double s = Surface(perp, te, fillFrac, axisLen, perpLen, p);
            switch (p.Direction)
            {
                case WaterDirection.Up: return axisPos > s;     // water at the bottom
                case WaterDirection.Down: return axisPos < s;   // water at the top
                case WaterDirection.Left: return axisPos < s;   // water at the left
                default: return axisPos > s;                    // Right: water at the right
            }
        }

        // Underwater refraction shimmer (light distortion): a gentle time-varying sinusoidal displacement.
        public static (double Dx, double Dy) Shimmer(double x, double y, double te, WaterParams p)
        {
            double a = p.RefractionStrength;
            double dx = a * Math.Sin(0.05 * y + te * 2.0) * 0.8 + a * 0.3 * Math.Sin(0.09 * x - te * 1.3);
            double dy = a * 0.4 * Math.Sin(0.06 * x + te * 1.7);
            return (dx, dy);
        }

        // Lens sampling scale at distance `dist` from a bubble centre of radius r: < 1 inside (magnifies),
        // ramping back to 1 at the rim. Pixels outside (dist > r) are unaffected (returns 1).
        public static double LensFactor(double dist, double r, double strength)
        {
            if (r <= 1e-6 || dist >= r) return 1.0;
            double t = dist / r;              // 0 centre .. 1 rim
            double mag = 1.0 - strength * (1.0 - t * t); // strongest magnification at the centre
            if (mag < 0.2) mag = 0.2;
            return mag;
        }

        // The bubbles visible at time te. Each belongs to a depth layer (auto near/far: far = small/faint/
        // weak lens). Bubbles drift toward the surface slowly with a left-right sway, swell a little as the
        // pressure drops near the surface, and fade out over a band before the waterline (so they never get
        // hard-clipped at the surface) — like a diver's exhaled bubbles drifting up and dissolving.
        public static WaterBubble[] Bubbles(double te, double fillFrac, int w, int h, WaterParams p)
        {
            if (fillFrac <= 0.0) return Array.Empty<WaterBubble>();
            int count = AutoBubbleCount(w, h);

            bool vertical = Vertical(p.Direction);
            bool fromLow = p.Direction == WaterDirection.Down || p.Direction == WaterDirection.Left;
            int axisLen = vertical ? h : w;
            int perpLen = vertical ? w : h;
            int layers = p.Layers < 1 ? 1 : p.Layers;

            var list = new System.Collections.Generic.List<WaterBubble>(count);
            for (int i = 0; i < count; i++)
            {
                int layer = i % layers;
                double depth = layers > 1 ? (double)layer / (layers - 1) : 1.0; // 0 far .. 1 near
                double baseR = p.BubbleSize * (0.4 + 0.9 * depth) * (0.7 + 0.6 * RainField.Hash01(i, 41, p.Seed));
                double riseSpeed = 0.12 * (0.55 + 0.9 * depth); // canvas fractions / sec (slow, leisurely)
                double opacity = 0.25 + 0.45 * depth;
                double lens = 0.2 + 0.6 * depth;
                double lane = RainField.Hash01(i, 42, p.Seed);
                double phase = RainField.Hash01(i, 43, p.Seed);

                double travel = phase + te * riseSpeed;
                double frac = travel - Math.Floor(travel); // 0..1 along the rise toward the surface

                double axisPos;
                switch (p.Direction)
                {
                    case WaterDirection.Up: axisPos = axisLen * (1.0 - frac); break;
                    case WaterDirection.Down: axisPos = axisLen * frac; break;
                    case WaterDirection.Left: axisPos = axisLen * frac; break;
                    default: axisPos = axisLen * (1.0 - frac); break; // Right
                }

                // Left-right drift: two slow sines for an organic, pressure-driven wobble.
                double swayAmp = perpLen * 0.05 * (0.5 + 0.8 * RainField.Hash01(i, 44, p.Seed));
                double ph2 = RainField.Hash01(i, 45, p.Seed) * 6.2831853;
                double sway = swayAmp * (0.7 * Math.Sin(te * 1.1 + ph2) + 0.3 * Math.Sin(te * 0.43 + i));
                double perpPos = lane * perpLen + sway;

                // How far below the surface the bubble centre is (along the fill axis).
                double surf = Surface(perpPos, te, fillFrac, axisLen, perpLen, p);
                double distToSurface = fromLow ? (surf - axisPos) : (axisPos - surf);
                if (distToSurface <= 0.0) continue;

                // Water pressure: swell a little as it nears the surface.
                double rise = 1.0 - Math.Min(1.0, distToSurface / axisLen);
                double r = baseR * (1.0 + 0.45 * rise);

                // Fade out over a band before the surface so it is gone before it would be clipped.
                double fadeBand = Math.Max(r * 2.0, axisLen * 0.14);
                double surfaceFade = (distToSurface - r) / fadeBand;
                if (surfaceFade <= 0.0) continue;
                if (surfaceFade > 1.0) surfaceFade = 1.0;
                double op = opacity * surfaceFade;
                if (op <= 0.01) continue;

                double bx = vertical ? perpPos : axisPos;
                double by = vertical ? axisPos : perpPos;
                list.Add(new WaterBubble { X = bx, Y = by, R = Math.Max(1.0, r), Opacity = op, Lens = lens });
            }
            return list.ToArray();
        }
    }
}
