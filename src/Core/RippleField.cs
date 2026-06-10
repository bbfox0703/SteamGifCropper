using System;

namespace GifProcessorApp
{
    // One water drop. Position can be anywhere — including outside the image (e.g. a 766x300 GIF with a
    // drop at 300,400); only the part of the ring that reaches a pixel affects it.
    public struct RippleDrop
    {
        public double X;            // centre x (px)
        public double Y;            // centre y (px)
        public double StartSeconds; // when the drop lands
        public double Intensity;    // this drop's amplitude multiplier (~1.0 nominal)

        public RippleDrop(double x, double y, double startSeconds, double intensity)
        {
            X = x;
            Y = y;
            StartSeconds = startSeconds;
            Intensity = intensity;
        }
    }

    // Shared "medium" — the water is the same for every drop, so these are global (not per-drop). Keeping
    // them shared is what makes the interference well-defined: every drop's wave travels at the same speed
    // and wavelength, so summing their displacements is physically meaningful.
    public struct RippleMedium
    {
        public double WaveSpeed;      // px/sec — how fast the ring front expands
        public double Wavelength;     // px — spacing between rings
        public double SpatialDamping; // 1/px — amplitude falloff with distance from the drop
        public double TimeDamping;    // 1/sec — amplitude falloff over time (ripple settles)
        public double Strength;       // px — global scale of the pixel displacement
        public double Threshold;      // amplitude below which a drop is considered gone (culled)
    }

    // Pure, dependency-free damped-radial-wave math (linked into the test project; RippleRenderer does the
    // Magick-heavy per-pixel resampling on top of this). A drop emits expanding concentric rings; a pixel
    // inside the wave front is pushed radially by the local wave height. Multiple drops sum their pushes,
    // so constructive/destructive interference emerges automatically.
    public static class RippleField
    {
        // Temporal envelope of a drop at elapsed time tau (seconds since it landed): the amplitude ceiling
        // that decays as the ripple settles. Zero before the drop lands.
        public static double Envelope(double intensity, double timeDamping, double tau)
        {
            if (tau < 0.0) return 0.0;
            if (timeDamping <= 0.0) return intensity; // no decay
            return intensity * Math.Exp(-timeDamping * tau);
        }

        // Lifetime (seconds) of a drop: how long until its envelope falls below the cull threshold.
        //   intensity * exp(-timeDamping * tau) = threshold  =>  tau = ln(intensity / threshold) / timeDamping
        public static double DropLifetime(double intensity, double timeDamping, double threshold)
        {
            if (threshold <= 0.0 || timeDamping <= 0.0) return double.PositiveInfinity;
            if (intensity <= threshold) return 0.0;
            return Math.Log(intensity / threshold) / timeDamping;
        }

        // Total scene length (seconds): the latest moment any drop is still alive (start + its lifetime).
        // Returns 0 if every drop is already below threshold or there are no drops.
        public static double TotalSeconds(RippleDrop[] drops, double timeDamping, double threshold)
        {
            if (drops == null) return 0.0;
            double max = 0.0;
            foreach (var d in drops)
            {
                double life = DropLifetime(d.Intensity, timeDamping, threshold);
                if (double.IsInfinity(life)) continue; // no temporal decay -> doesn't bound the scene
                double end = d.StartSeconds + life;
                if (end > max) max = end;
            }
            return max;
        }

        // Effective scene/window length: never shorter than the user's duration, but always long enough to
        // cover every drop's start + lifetime — so a drop landing after `duration` (e.g. drops at 15.5s
        // with the default 4s duration) is not silently swallowed. Drops with no temporal decay have an
        // unbounded life and don't extend it (TotalSeconds skips them); the duration cap still applies.
        public static double EffectiveDuration(double duration, RippleDrop[] drops, RippleMedium m)
        {
            return Math.Max(duration, TotalSeconds(drops, m.TimeDamping, m.Threshold));
        }

        // Whether any drop is contributing at time t (landed and not yet faded below threshold). Lets the
        // caller skip the per-pixel resample for frames where nothing is rippling (just copy the source).
        public static bool AnyDropActive(double t, RippleDrop[] drops, RippleMedium m)
        {
            if (drops == null) return false;
            foreach (var d in drops)
            {
                if (t < d.StartSeconds) continue;
                double life = DropLifetime(d.Intensity, m.TimeDamping, m.Threshold);
                if (double.IsInfinity(life) || t <= d.StartSeconds + life) return true;
            }
            return false;
        }

        // Radial pixel displacement (dx, dy) at pixel (x, y) and absolute time t (seconds), summed over all
        // drops (interference). Each live drop within its expanding front pushes the pixel along the radial
        // direction by its local wave height; the result is scaled by the medium strength (px).
        public static (double Dx, double Dy) Displacement(double x, double y, double t, RippleDrop[] drops, RippleMedium m)
        {
            if (drops == null || drops.Length == 0) return (0.0, 0.0);

            double k = (m.Wavelength > 1e-6) ? (2.0 * Math.PI / m.Wavelength) : 0.0;
            double omega = m.WaveSpeed * k;

            double dx = 0.0, dy = 0.0;
            for (int i = 0; i < drops.Length; i++)
            {
                RippleDrop d = drops[i];
                double tau = t - d.StartSeconds;
                if (tau <= 0.0) continue; // hasn't landed yet

                double env = Envelope(d.Intensity, m.TimeDamping, tau);
                if (env < m.Threshold) continue; // this drop has faded out

                double rx = x - d.X;
                double ry = y - d.Y;
                double r = Math.Sqrt(rx * rx + ry * ry);
                if (r < 1e-6) continue;          // at the centre there is no radial direction
                if (r > m.WaveSpeed * tau) continue; // the front hasn't reached this pixel yet (causality)

                // Wave height here: time envelope * distance falloff * the ring oscillation. The phase
                // k*(r - waveSpeed*tau) is 0 at the front and oscillates inward, so the rings travel out.
                double amp = env * Math.Exp(-m.SpatialDamping * r) * Math.Sin(k * r - omega * tau);

                double inv = 1.0 / r;
                dx += amp * rx * inv; // radial unit vector * amplitude
                dy += amp * ry * inv;
            }

            return (m.Strength * dx, m.Strength * dy);
        }
    }
}
