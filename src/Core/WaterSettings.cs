using System;

namespace GifProcessorApp
{
    // Parameters captured from WaterDialog and consumed by the GifProcessor water engine. The canvas fills
    // with water from a chosen direction between StartSeconds and FullSeconds; below the wavy surface the
    // image is refracted and seen through rising, lensing bubbles, above it nothing changes. Output is a
    // single full-width 766px GIF (no auto-split), chainable. BuildSettings() does the control->settings map.
    public class WaterSettings
    {
        public string InputFilePath { get; set; }
        public string OutputFilePath { get; set; }
        public bool IsGif { get; set; }

        // Scene
        public int Fps { get; set; } = 20;
        public double DurationSeconds { get; set; } = 6.0;    // frozen / static mode length
        public bool PlayGifDuringWater { get; set; } = true;  // GIF: fill over live frames vs frozen frame 0
        public bool KeepOriginalSize { get; set; } = false;

        // Water
        public WaterDirection Direction { get; set; } = WaterDirection.Up;
        public double StartSeconds { get; set; } = 0.5;        // when the water starts rising
        public double FullSeconds { get; set; } = 4.0;         // when the water reaches the full level
        public double FadeOutSeconds { get; set; } = 0.5;      // after full, fade the whole effect out (0 = stay)
        public double RefractionStrength { get; set; } = 4.0;  // px shimmer
        public double SurfaceWobble { get; set; } = 6.0;       // px surface wave
        public double BubbleSize { get; set; } = 12.0;         // px base radius (count is auto)
        public int Layers { get; set; } = 3;                   // depth layers (>= 3)
        public int Seed { get; set; } = 20240601;

        public WaterParams ToParams()
        {
            return new WaterParams
            {
                Direction = Direction,
                RefractionStrength = RefractionStrength,
                SurfaceWobble = SurfaceWobble,
                BubbleSize = BubbleSize,
                Layers = Math.Max(1, Layers),
                Seed = Seed,
            };
        }
    }
}
