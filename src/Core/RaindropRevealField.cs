using System;

namespace GifProcessorApp
{
    // One raindrop in the reveal: born at BirthT (morph progress in [0,1)), lands at (Px,Py), and its
    // puddle grows to radius MaxR by the end of the window.
    public struct RaindropSeed
    {
        public double BirthT;
        public double Px, Py;
        public double MaxR;

        public RaindropSeed(double birthT, double px, double py, double maxR)
        {
            BirthT = birthT; Px = px; Py = py; MaxR = maxR;
        }
    }

    // Pure, dependency-free math for the raindrop-reveal morph (linked into the test project;
    // RaindropRevealRenderer does the Magick blend on top). Coverage(x,y,t) in [0,1] is the fraction of
    // clip B shown at that pixel: 0 = all A, 1 = all B. As t advances the puddles grow (and a small global
    // floor near the end guarantees a fully-B frame at t=1), so coverage is monotonically increasing.
    public static class RaindropRevealField
    {
        // Build the deterministic drop schedule for a given canvas. Drops are born within the first 85% of
        // the window so each has time to spread; positions and sizes come from the seeded hash.
        public static RaindropSeed[] BuildDrops(MorphSettings s, int w, int h)
        {
            int count = (int)Math.Round(s.RainIntensity);
            if (count < 1) count = 1;
            var drops = new RaindropSeed[count];
            double var = s.DropSizeVariationPct / 100.0;
            for (int i = 0; i < count; i++)
            {
                double birth = RainField.Hash01(i, 11, s.Seed) * 0.85;
                double px = RainField.Hash01(i, 12, s.Seed) * w;
                double py = RainField.Hash01(i, 13, s.Seed) * h;
                double sizeFactor = 1.0 + (RainField.Hash01(i, 14, s.Seed) * 2.0 - 1.0) * var;
                if (sizeFactor < 0.1) sizeFactor = 0.1;
                drops[i] = new RaindropSeed(birth, px, py, Math.Max(1.0, s.SpreadRadius * sizeFactor));
            }
            return drops;
        }

        public static double SmoothStep(double x)
        {
            if (x <= 0.0) return 0.0;
            if (x >= 1.0) return 1.0;
            return x * x * (3.0 - 2.0 * x);
        }

        // Global B floor that ramps 0 -> 1 over the last 15% of the window, so the frame is guaranteed
        // fully B at t = 1 regardless of where the drops happened to land.
        public static double GlobalFloor(double t)
        {
            return SmoothStep((t - 0.85) / 0.15);
        }

        // Fraction of B shown at pixel (x,y) at morph progress t (in [0,1]). Union (max) of each born
        // drop's soft disc, raised to the global floor. `soft` is the edge feather in px.
        public static double Coverage(double x, double y, double t, RaindropSeed[] drops, double soft)
        {
            double cov = GlobalFloor(t);
            if (cov >= 1.0) return 1.0;
            double s = soft <= 1e-3 ? 1e-3 : soft;
            if (drops != null)
            {
                for (int i = 0; i < drops.Length; i++)
                {
                    RaindropSeed d = drops[i];
                    if (t <= d.BirthT) continue;
                    double age = (t - d.BirthT) / Math.Max(1e-3, 1.0 - d.BirthT); // 0..1 over remaining window
                    double r = d.MaxR * SmoothStep(age);
                    double dx = x - d.Px, dy = y - d.Py;
                    double dist = Math.Sqrt(dx * dx + dy * dy);
                    double v = (r - dist) / s + 0.5; // soft edge: >1 deep inside, <0 outside
                    if (v <= cov) continue;
                    double cv = v >= 1.0 ? 1.0 : v;
                    if (cv > cov) cov = cv;
                    if (cov >= 1.0) return 1.0;
                }
            }
            return cov < 0.0 ? 0.0 : cov;
        }
    }
}
