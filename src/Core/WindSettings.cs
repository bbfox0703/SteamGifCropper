using System;
using System.Collections.Generic;

namespace GifProcessorApp
{
    // How the wind event is authored. Normal = up to 3 gusts that share one travel direction. Nuclear =
    // a single scripted event (forward blast -> still gap -> strong reverse wind); the reverse leg is
    // the lone case where the travel direction flips.
    public enum WindMode
    {
        Normal = 0,
        Nuclear = 1,
    }

    // Parameters captured from WindDialog and consumed by the GifProcessor wind engine. The medium
    // (direction / wavelength / speed / strength / bend / flutter) is shared across all gusts; each gust
    // carries only when / how long / how hard. Output is a single full-width 766px GIF (no auto-split),
    // chainable; split later with the main "Split GIF" button.
    public class WindSettings
    {
        public string InputFilePath { get; set; }
        public string OutputFilePath { get; set; }
        public bool IsGif { get; set; }

        // Scene
        public int Fps { get; set; } = 20;
        public double DurationSeconds { get; set; } = 6.0;     // total effect length (2dp)
        public double EffectStartSeconds { get; set; } = 0.0;  // GIF "play during" only: where the window begins
        public bool PlayGifDuringWind { get; set; } = true;    // GIF: wind over live frames vs frozen frame 0
        public bool KeepOriginalSize { get; set; } = false;    // true: process at native size (no 766px fit)

        // Shared medium
        public WindFromDirection Direction { get; set; } = WindFromDirection.Left;
        public double Wavelength { get; set; } = 120.0;  // px
        public double WaveSpeed { get; set; } = 300.0;   // px/sec
        public double Strength { get; set; } = 10.0;     // px displacement scale
        public double BendRatio { get; set; } = 0.4;     // steady lean into the wind
        public double FlutterRatio { get; set; } = 0.2;  // perpendicular jitter

        public WindMode Mode { get; set; } = WindMode.Normal;

        // Normal mode: up to 3 gusts (only the ticked ones are added by the dialog).
        public List<WindGust> Gusts { get; set; } = new List<WindGust>();

        // Nuclear mode: a single event expanded into two internal gusts by ResolveGusts().
        public double NukeBlastStrength { get; set; } = 1.2;
        public double NukeBlastDuration { get; set; } = 0.4;
        public double NukeGap { get; set; } = 0.6;
        public double NukeReverseStrength { get; set; } = 2.0;
        public double NukeReverseDuration { get; set; } = 4.0;

        public WindMedium ToMedium()
        {
            var (dx, dy) = WindField.DirectionVector(Direction);
            return new WindMedium
            {
                DirX = dx,
                DirY = dy,
                Wavelength = Wavelength,
                WaveSpeed = WaveSpeed,
                Strength = Strength,
                BendRatio = BendRatio,
                FlutterRatio = FlutterRatio,
            };
        }

        // The gust list fed to WindField. Normal returns the user's gusts as-is. Nuclear scripts the
        // single event: a short forward blast, a still gap, then a long strong reverse wind (the reverse
        // leg is the only place the travel direction flips).
        public WindGust[] ResolveGusts()
        {
            if (Mode == WindMode.Nuclear)
            {
                double blast = Math.Max(0.05, NukeBlastDuration);
                double gap = Math.Max(0.0, NukeGap);
                double rev = Math.Max(0.05, NukeReverseDuration);
                return new[]
                {
                    new WindGust(0.0, blast, NukeBlastStrength, false),
                    new WindGust(blast + gap, rev, NukeReverseStrength, true),
                };
            }
            return Gusts != null ? Gusts.ToArray() : new WindGust[0];
        }
    }
}
