using System;

namespace GifProcessorApp
{
    // Which A->B morph to run during the transition window.
    public enum MorphStyle
    {
        RaindropReveal = 0, // raindrops fall on A; their spreading puddles cross-dissolve to B
        TileFlip = 1,       // the canvas is split into cells that flip A->B one by one
    }

    // Tile-flip squash axis / sense. Up/Down squash vertically, Left/Right horizontally; Random picks a
    // per-cell axis. Enum order matches the dialog's direction dropdown index.
    public enum TileFlipDirection
    {
        Random = 0,
        Up = 1,
        Down = 2,
        Left = 3,
        Right = 4,
    }

    // Parameters for the A->B "morph" transition (its own timeline, distinct from the concat overlap
    // model): A plays for PreRollSeconds, then A morphs into B over MorphSeconds (A is fully gone by the
    // end and its leftover is discarded), then B's remaining footage plays to the end. Output is a single
    // full-width 766px GIF (no auto-split), chainable. BuildSettings() does the control->settings mapping.
    public class MorphSettings
    {
        public string InputAPath { get; set; }
        public string InputBPath { get; set; }
        public string OutputPath { get; set; }

        public MorphStyle Style { get; set; } = MorphStyle.RaindropReveal;
        public double PreRollSeconds { get; set; } = 2.0;   // A alone before the morph
        public double MorphSeconds { get; set; } = 3.0;     // length of the A->B morph window
        public int Fps { get; set; } = 20;                  // morph-window frame rate (A/B resampled to it)
        public bool KeepOriginalSize { get; set; } = false; // true: use A's size; false: fit to 766px
        public int Seed { get; set; } = 20240601;

        // Raindrop-reveal style.
        public double RainIntensity { get; set; } = 30.0;        // number of drops over the window
        public double DropSizeVariationPct { get; set; } = 40.0; // +/- variation of each drop's max radius
        public double SpreadRadius { get; set; } = 90.0;         // px: base max puddle radius
        public double SpreadVariationPct { get; set; } = 40.0;   // (reserved) extra growth variation
        public double SoftEdge { get; set; } = 8.0;              // px feather of the puddle edge

        // Tile-flip style.
        public int Divisions { get; set; } = 8;                                       // cells across the width
        public TileFlipDirection FlipDirection { get; set; } = TileFlipDirection.Random;
    }

    // Pure timeline math for the morph (linked into the test project; GifProcessor.Morph uses it to size
    // the phases). The defining identity is total = preRoll + bDur whenever morph <= bDur.
    public static class MorphTimeline
    {
        // Clamp the morph length so it never exceeds B's length (no point morphing past the end of B).
        public static double ClampMorph(double morphSeconds, double bDur)
        {
            double m = morphSeconds;
            if (m < 0.0) m = 0.0;
            if (m > bDur) m = bDur;
            return m;
        }

        // Total output length (seconds): preRoll(A) + morph + remaining(B).
        public static double TotalSeconds(double preRoll, double morphSeconds, double aDur, double bDur)
        {
            double pr = preRoll < 0.0 ? 0.0 : preRoll;
            double m = ClampMorph(morphSeconds, bDur);
            double remB = bDur - m;
            if (remB < 0.0) remB = 0.0;
            return pr + m + remB; // == pr + bDur for m <= bDur
        }
    }
}
