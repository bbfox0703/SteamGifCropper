using System;

namespace GifProcessorApp
{
    // The 8 compass directions the wind comes FROM. The wave field travels toward the opposite side
    // (e.g. Left -> the gust front sweeps rightward across the canvas). Enum order matches the dialog's
    // direction dropdown index, so (WindFromDirection)cmbDirection.SelectedIndex is valid.
    public enum WindFromDirection
    {
        Left = 0,         // travels right       (+1,  0)
        Right = 1,        // travels left        (-1,  0)
        Top = 2,          // travels down        ( 0, +1)
        Bottom = 3,       // travels up          ( 0, -1)
        TopLeft = 4,      // travels down-right
        TopRight = 5,     // travels down-left
        BottomLeft = 6,   // travels up-right
        BottomRight = 7,  // travels up-left
    }

    // One gust of wind: a temporal burst (start + length) at a given strength. The travel direction is
    // shared via the medium; a gust may travel reversed (Nuclear mode's backdraft) by setting Reverse.
    public struct WindGust
    {
        public double StartSeconds;     // when the gust begins (seconds, relative to the effect window)
        public double DurationSeconds;  // how long the gust blows
        public double Intensity;        // this gust's amplitude multiplier (~1.0 nominal)
        public bool Reverse;            // travel along -d instead of +d (only Nuclear uses this)

        public WindGust(double startSeconds, double durationSeconds, double intensity, bool reverse = false)
        {
            StartSeconds = startSeconds;
            DurationSeconds = durationSeconds;
            Intensity = intensity;
            Reverse = reverse;
        }
    }

    // Shared "air" — the wave medium is the same for every gust (so overlapping gusts interfere
    // coherently). The travel direction is global; gusts only modulate amplitude over time.
    public struct WindMedium
    {
        public double DirX;          // travel unit vector x (+x right)
        public double DirY;          // travel unit vector y (+y down)
        public double Wavelength;    // px — spacing between sway crests along the travel axis
        public double WaveSpeed;     // px/sec — how fast a crest travels across the field
        public double Strength;      // px — global scale of the pixel displacement
        public double BendRatio;     // steady lean into the wind, relative to the rolling wave amplitude
        public double FlutterRatio;  // small perpendicular jitter, relative to the rolling wave amplitude
    }

    // Pure, dependency-free wind-sway math (linked into the test project; WindRenderer does the
    // Magick-heavy per-pixel resampling on top of this). A gust is a plane wave travelling along the
    // wind direction, windowed in time by a Hann bump (rises, peaks, fades). Multiple gusts sum their
    // displacements, so overlapping gusts produce turbulence automatically. The look is wind rolling
    // over a wheat field: each pixel leans + ripples along the wind while a gust passes over it.
    public static class WindField
    {
        // Travel unit vector for a "wind comes from" direction.
        public static (double X, double Y) DirectionVector(WindFromDirection from)
        {
            const double s = 0.70710678118654752; // 1/sqrt(2)
            switch (from)
            {
                case WindFromDirection.Left: return (1.0, 0.0);
                case WindFromDirection.Right: return (-1.0, 0.0);
                case WindFromDirection.Top: return (0.0, 1.0);
                case WindFromDirection.Bottom: return (0.0, -1.0);
                case WindFromDirection.TopLeft: return (s, s);
                case WindFromDirection.TopRight: return (-s, s);
                case WindFromDirection.BottomLeft: return (s, -s);
                case WindFromDirection.BottomRight: return (-s, -s);
                default: return (1.0, 0.0);
            }
        }

        // Temporal envelope of a gust at elapsed time tau (seconds since it began): a Hann (raised-sine)
        // bump that is 0 at tau=0, peaks at tau=duration/2, and returns to 0 at tau=duration; zero
        // outside [0, duration]. This is what makes a gust swell in and die out like real wind.
        public static double GustEnvelope(double tau, double duration)
        {
            if (duration <= 0.0) return 0.0;
            if (tau <= 0.0 || tau >= duration) return 0.0;
            double s = Math.Sin(Math.PI * tau / duration);
            return s * s; // sin^2 == Hann window
        }

        // Total scene length (seconds): the latest moment any gust is still blowing (start + duration).
        // Returns 0 if there are no gusts.
        public static double TotalSeconds(WindGust[] gusts)
        {
            if (gusts == null) return 0.0;
            double max = 0.0;
            foreach (var g in gusts)
            {
                double end = g.StartSeconds + g.DurationSeconds;
                if (end > max) max = end;
            }
            return max;
        }

        // Whether any gust is blowing at time t (inside its [start, start+duration] window with non-zero
        // intensity). Lets the caller skip the per-pixel resample for frames where the air is still
        // (just copy the source frame through unchanged).
        public static bool AnyGustActive(double t, WindGust[] gusts)
        {
            if (gusts == null) return false;
            foreach (var g in gusts)
            {
                double tau = t - g.StartSeconds;
                if (tau > 0.0 && tau < g.DurationSeconds && g.Intensity != 0.0) return true;
            }
            return false;
        }

        // Pixel displacement (dx, dy) at pixel (x, y) and absolute time t (seconds), summed over all
        // gusts. Each blowing gust contributes a travelling plane wave: the phase k*(p·d) - omega*tau
        // rolls crests across the field along the wind direction d. The pixel moves along d (a steady
        // lean plus the rolling ripple) with a small perpendicular flutter; everything is enveloped by
        // the gust's Hann bump and scaled by the medium strength (px).
        public static (double Dx, double Dy) Displacement(double x, double y, double t, WindGust[] gusts, WindMedium m)
        {
            if (gusts == null || gusts.Length == 0) return (0.0, 0.0);

            double k = (m.Wavelength > 1e-6) ? (2.0 * Math.PI / m.Wavelength) : 0.0;
            double omega = m.WaveSpeed * k;

            double dx = 0.0, dy = 0.0;
            for (int i = 0; i < gusts.Length; i++)
            {
                WindGust g = gusts[i];
                double tau = t - g.StartSeconds;
                if (tau <= 0.0 || tau >= g.DurationSeconds) continue; // not blowing now

                double env = g.Intensity * GustEnvelope(tau, g.DurationSeconds);
                if (env == 0.0) continue;

                // Travel direction for this gust (reverse = backdraft in Nuclear mode).
                double ddx = g.Reverse ? -m.DirX : m.DirX;
                double ddy = g.Reverse ? -m.DirY : m.DirY;

                double s = x * ddx + y * ddy;            // projection onto the travel axis (phase coord)
                double phase = k * s - omega * tau;
                double roll = Math.Sin(phase);           // the rolling wave crossing the field
                double flutter = Math.Sin(2.0 * phase);  // higher-frequency perpendicular jitter

                double along = env * (m.BendRatio + roll);    // steady lean + rolling wave, along the wind
                double perp = env * m.FlutterRatio * flutter; // small sideways wiggle for a natural look

                // perp(d) = (-ddy, ddx)
                dx += along * ddx + perp * (-ddy);
                dy += along * ddy + perp * (ddx);
            }

            return (m.Strength * dx, m.Strength * dy);
        }
    }
}
