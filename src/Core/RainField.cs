using System;

namespace GifProcessorApp
{
    // Which way the wind pushes the rain. None = straight down; Left/Right add a lateral drift so the
    // streaks slant. Enum order matches the dialog's direction dropdown index, so
    // (RainWindDirection)cmbWindDir.SelectedIndex is valid.
    public enum RainWindDirection
    {
        None = 0,
        Left = 1,
        Right = 2,
    }

    // One rain streak for a single frame: the drop head at (X0,Y0) with a motion-blur tail trailing back
    // to (X1,Y1) (the tail is where the drop was a moment ago, i.e. opposite its velocity).
    public struct RainStreak
    {
        public double X0, Y0; // head (leading point, lower on screen)
        public double X1, Y1; // tail (trailing point)

        public RainStreak(double x0, double y0, double x1, double y1)
        {
            X0 = x0; Y0 = y0; X1 = x1; Y1 = y1;
        }
    }

    // Resolved per-canvas rain parameters. RainSettings.ToParams() builds this once the canvas size is
    // known (the streak count scales with the canvas area).
    public struct RainParams
    {
        public int Count;            // number of simultaneous streaks
        public double FallSpeed;     // px/sec downward (base; per-drop varied)
        public double WindX;         // px/sec lateral drift (signed: +right, -left)
        public double StreakLength;  // px (base; per-drop varied)
        public int Seed;             // deterministic layout seed
    }

    // Pure, dependency-free rain math (linked into the test project; RainRenderer rasterizes the streaks
    // onto a frame). Drops fall continuously and wrap over the canvas, so any time slice is a valid full
    // rain field and the motion loops seamlessly. The look is a translucent overlay of slanted streaks.
    public static class RainField
    {
        // Deterministic pseudo-random value in [0,1) for drop i / channel salt / seed. No RNG state, so
        // Streaks() stays pure and reproducible run-to-run.
        public static double Hash01(int i, int salt, int seed)
        {
            unchecked
            {
                uint x = (uint)(i * 73856093) ^ (uint)(salt * 19349663) ^ (uint)(seed * 83492791);
                x ^= x >> 13;
                x *= 0x5bd1e995u;
                x ^= x >> 15;
                return (x & 0xFFFFFFu) / (double)0x1000000;
            }
        }

        // Number of streaks for a 0..100 "rain amount" over a canvas, scaled by area and clamped to a
        // sane range so high settings stay performant.
        public static int DropCount(double rainAmount, int w, int h)
        {
            double amt = rainAmount;
            if (amt < 0.0) amt = 0.0; else if (amt > 100.0) amt = 100.0;
            double area = (double)Math.Max(1, w) * Math.Max(1, h);
            int max = (int)(area / 500.0);
            if (max < 40) max = 40; else if (max > 1500) max = 1500;
            return (int)Math.Round(amt / 100.0 * max);
        }

        // Whole-layer opacity at elapsed time te (seconds within the effect window): 1 normally; if a
        // "rain stops" fade-out is enabled, ramps 1 -> 0 linearly over the last fadeSeconds of the window.
        public static double FadeAlpha(double te, double winDur, bool fadeOut, double fadeSeconds)
        {
            if (te < 0.0 || te > winDur) return 0.0;
            if (!fadeOut || fadeSeconds <= 0.0) return 1.0;
            double fs = fadeSeconds;
            if (fs > winDur) fs = winDur;
            double fadeStart = winDur - fs;
            if (te <= fadeStart) return 1.0;
            double a = 1.0 - (te - fadeStart) / fs; // 0..1 across the fade
            return a < 0.0 ? 0.0 : a;
        }

        // Whether the rain layer is visible at te (inside the window with non-zero opacity). Lets the
        // caller skip the overlay for frames where there is nothing to draw (pass the source through).
        public static bool AnyRainActive(double te, double winDur, bool fadeOut, double fadeSeconds)
        {
            return FadeAlpha(te, winDur, fadeOut, fadeSeconds) > 0.0;
        }

        // The streaks for absolute time t (seconds). Heads wrap over the canvas (+ a small top margin so
        // drops enter from above); each streak trails back along its velocity by StreakLength.
        public static RainStreak[] Streaks(double t, int w, int h, RainParams p)
        {
            int count = p.Count < 0 ? 0 : p.Count;
            var list = new RainStreak[count];
            double width = Math.Max(1.0, w);
            const double margin = 40.0;
            double height = Math.Max(1.0, h + margin); // wrap height; streaks start above the top
            for (int i = 0; i < count; i++)
            {
                double rx = Hash01(i, 1, p.Seed);
                double rp = Hash01(i, 2, p.Seed);
                double rs = 0.7 + 0.6 * Hash01(i, 3, p.Seed); // speed factor 0.7..1.3
                double rl = 0.7 + 0.6 * Hash01(i, 4, p.Seed); // length factor
                double vy = p.FallSpeed * rs;
                double vx = p.WindX * rs;
                double headY = Mod(rp * height + t * vy, height) - margin;
                double headX = Mod(rx * width + t * vx, width);
                double speed = Math.Sqrt(vx * vx + vy * vy);
                double ux = speed > 1e-6 ? vx / speed : 0.0;
                double uy = speed > 1e-6 ? vy / speed : 1.0;
                double len = p.StreakLength * rl;
                list[i] = new RainStreak(headX, headY, headX - ux * len, headY - uy * len);
            }
            return list;
        }

        private static double Mod(double a, double m)
        {
            double r = a % m;
            return r < 0.0 ? r + m : r;
        }
    }
}
