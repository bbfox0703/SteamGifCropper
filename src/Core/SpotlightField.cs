using System;

namespace GifProcessorApp
{
    // Resolved spotlight parameters for the morph (built from MorphSettings once the canvas size is known).
    public struct SpotlightParams
    {
        public double Radius;        // px: spotlight circle radius during the moving phase
        public double Speed;         // px/sec: how fast the spotlight travels
        public double ExpandSeconds; // seconds at the end of the window over which the circle grows to fill
        public double Soft;          // px: edge feather
        public int Seed;             // deterministic path seed
    }

    // Pure, dependency-free math for the spotlight morph (linked into the test project; SpotlightRenderer
    // does the Magick blend). A circular spotlight sweeps over clip A like a billiard ball (constant
    // velocity, reflecting off the four edges), revealing clip B only where it shines. In the final
    // ExpandSeconds the circle freezes and grows until it covers the whole canvas, so the frame ends fully
    // B. The reveal is NOT cumulative — when the light moves on, the area it left returns to A; the expand
    // is what completes the transition.
    public static class SpotlightField
    {
        public static double SmoothStep(double x)
        {
            if (x <= 0.0) return 0.0;
            if (x >= 1.0) return 1.0;
            return x * x * (3.0 - 2.0 * x);
        }

        // Radius clamped so the circle fits inside the canvas (its centre can still reach every edge).
        public static double ClampRadius(double radius, int w, int h)
        {
            double maxR = Math.Min(w, h) / 2.0 - 1.0;
            if (maxR < 1.0) maxR = 1.0;
            double r = radius;
            if (r < 1.0) r = 1.0;
            if (r > maxR) r = maxR;
            return r;
        }

        // Normalised morph progress (0..1) at which the expand phase begins.
        public static double ExpandFrac(double expandSeconds, double morphSeconds)
        {
            if (morphSeconds <= 1e-6) return 0.0;
            double e = expandSeconds < 0.0 ? 0.0 : expandSeconds;
            double f = 1.0 - e / morphSeconds;
            if (f < 0.0) f = 0.0;
            if (f > 1.0) f = 1.0;
            return f;
        }

        // 1D billiard bounce of a point in [lo, hi]: starts at startFrac (0..1 across the span) and travels
        // at velocity v (px/sec); reflects off both ends. Analytic (triangle wave), so it is pure and has
        // no per-step drift.
        public static double Bounce(double startFrac, double v, double tSec, double lo, double hi)
        {
            double span = hi - lo;
            if (span <= 1e-6) return lo;
            double p0 = startFrac * span;
            double p = p0 + v * tSec;
            double period = 2.0 * span;
            double m = p % period;
            if (m < 0.0) m += period;
            if (m > span) m = period - m; // reflect
            return lo + m;
        }

        // Spotlight centre at normalised morph progress tNorm (frozen once the expand phase begins, so the
        // circle grows from a fixed point).
        public static (double Cx, double Cy) Center(double tNorm, double morphSeconds, int w, int h, SpotlightParams p)
        {
            double r = ClampRadius(p.Radius, w, h);
            double expandFrac = ExpandFrac(p.ExpandSeconds, morphSeconds);
            double moveNorm = Math.Min(tNorm, expandFrac);
            double tau = moveNorm * morphSeconds; // seconds of travel

            double ang = RainField.Hash01(0, 31, p.Seed) * 2.0 * Math.PI;
            double vx = p.Speed * Math.Cos(ang);
            double vy = p.Speed * Math.Sin(ang) * (0.6 + 0.8 * RainField.Hash01(0, 32, p.Seed)); // skew so x/y periods differ
            double sx = RainField.Hash01(0, 33, p.Seed);
            double sy = RainField.Hash01(0, 34, p.Seed);

            double cx = Bounce(sx, vx, tau, r, w - r);
            double cy = Bounce(sy, vy, tau, r, h - r);
            return (cx, cy);
        }

        // Spotlight radius at tNorm: constant during the moving phase, then growing to the canvas diagonal
        // by tNorm = 1 (which covers the whole canvas from any centre, guaranteeing a fully-B final frame).
        public static double RadiusAt(double tNorm, double morphSeconds, int w, int h, SpotlightParams p)
        {
            double r = ClampRadius(p.Radius, w, h);
            double expandFrac = ExpandFrac(p.ExpandSeconds, morphSeconds);
            if (tNorm <= expandFrac) return r;
            double prog = (tNorm - expandFrac) / Math.Max(1e-6, 1.0 - expandFrac);
            if (prog > 1.0) prog = 1.0;
            double maxR = Math.Sqrt((double)w * w + (double)h * h);
            return r + (maxR - r) * SmoothStep(prog);
        }

        // Soft circle coverage at pixel (x,y): 1 inside the circle, 0 outside, feathered over `soft` px.
        public static double Coverage(double x, double y, double cx, double cy, double radius, double soft)
        {
            double dx = x - cx, dy = y - cy;
            double dist = Math.Sqrt(dx * dx + dy * dy);
            double s = soft <= 1e-3 ? 1e-3 : soft;
            double v = (radius - dist) / s + 0.5;
            if (v <= 0.0) return 0.0;
            if (v >= 1.0) return 1.0;
            return v;
        }
    }
}
