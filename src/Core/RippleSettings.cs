using System.Collections.Generic;

namespace GifProcessorApp
{
    // Parameters captured from RippleDialog and consumed by the GifProcessor ripple engine. The medium
    // (wave speed / wavelength / damping / strength / threshold) is shared across all drops — that is what
    // makes the interference well-defined — while each RippleDrop carries only where / when / how hard.
    // Output is a single full-width 766px GIF (no auto-split), chainable; split later with "Split GIF".
    public class RippleSettings
    {
        public string InputFilePath { get; set; }
        public string OutputFilePath { get; set; }
        public bool IsGif { get; set; }

        // Scene
        public int Fps { get; set; } = 20;
        public double DurationSeconds { get; set; } = 4.0;     // total scene length (2dp)
        public bool PlayGifDuringRipple { get; set; } = true;  // GIF: ripple over live frames vs frozen frame 0
        public bool KeepOriginalSize { get; set; } = false;    // true: process at native size (no 766px fit)

        // Medium (shared)
        public double WaveSpeed { get; set; } = 220.0;     // px/sec
        public double Wavelength { get; set; } = 36.0;     // px
        public double SpatialDamping { get; set; } = 0.004; // 1/px
        public double TimeDamping { get; set; } = 0.8;     // 1/sec
        public double Strength { get; set; } = 8.0;        // px displacement scale
        public double Threshold { get; set; } = 0.03;      // cull amplitude

        // Up to 3 drops.
        public List<RippleDrop> Drops { get; set; } = new List<RippleDrop>();

        public RippleMedium ToMedium()
        {
            return new RippleMedium
            {
                WaveSpeed = WaveSpeed,
                Wavelength = Wavelength,
                SpatialDamping = SpatialDamping,
                TimeDamping = TimeDamping,
                Strength = Strength,
                Threshold = Threshold,
            };
        }
    }
}
