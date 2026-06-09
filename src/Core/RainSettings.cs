using System;

namespace GifProcessorApp
{
    // Parameters captured from RainDialog and consumed by the GifProcessor rain engine. A translucent
    // rain layer is drawn over the source (no pixel displacement). Output is a single full-width 766px
    // GIF (no auto-split), chainable; split later with the main "Split GIF" button. BuildSettings() does
    // the control->settings mapping in one place so a new field can't be silently dropped on the way out.
    public class RainSettings
    {
        public string InputFilePath { get; set; }
        public string OutputFilePath { get; set; }
        public bool IsGif { get; set; }

        // Scene
        public int Fps { get; set; } = 20;
        public double DurationSeconds { get; set; } = 6.0;     // effect length (2dp)
        public double EffectStartSeconds { get; set; } = 0.0;  // GIF "play during" only: where the window begins
        public bool PlayGifDuringRain { get; set; } = true;    // GIF: rain over live frames vs frozen frame 0
        public bool KeepOriginalSize { get; set; } = false;    // true: process at native size (no 766px fit)

        // Rain
        public double RainAmount { get; set; } = 40.0;                                 // 0..100 intensity
        public RainWindDirection WindDirection { get; set; } = RainWindDirection.None;
        public double WindStrength { get; set; } = 150.0;                              // px/sec lateral drift magnitude
        public double DropLength { get; set; } = 16.0;                                 // px streak length (base)
        public int Seed { get; set; } = 20240601;

        // "Rain stops" fade-out at the end of the window.
        public bool FadeOut { get; set; } = false;
        public double FadeOutSeconds { get; set; } = 1.0;

        // Resolve the size-independent dialog values into per-canvas rain parameters.
        public RainParams ToParams(int w, int h)
        {
            double windX = 0.0;
            if (WindDirection == RainWindDirection.Left) windX = -WindStrength;
            else if (WindDirection == RainWindDirection.Right) windX = WindStrength;

            return new RainParams
            {
                Count = RainField.DropCount(RainAmount, w, h),
                FallSpeed = Math.Max(120.0, h * 1.2), // px/sec; faster on taller canvases
                WindX = windX,
                StreakLength = DropLength,
                Seed = Seed,
            };
        }
    }
}
