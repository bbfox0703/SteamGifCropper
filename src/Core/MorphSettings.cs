using System;

namespace GifProcessorApp
{
    // Which A->B morph to run during the transition window.
    public enum MorphStyle
    {
        RaindropReveal = 0, // raindrops fall on A; their spreading puddles cross-dissolve to B
        TileFlip = 1,       // the canvas is split into cells that flip A->B one by one
        Spotlight = 2,      // a bouncing searchlight reveals B, then expands to fill the frame
        Jigsaw = 3,         // puzzle pieces fill in A->B one by one, with fading boundary lines
        Brick = 4,          // planks of B drop in and stack over A with an impact bounce
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

        // Tile-flip style (Divisions is also reused by the jigsaw style for the piece count).
        public int Divisions { get; set; } = 8;                                       // cells across the width
        public TileFlipDirection FlipDirection { get; set; } = TileFlipDirection.Random;

        // Spotlight style.
        public double SpotlightRadius { get; set; } = 120.0;       // px: circle radius while moving
        public double SpotlightSpeed { get; set; } = 400.0;        // px/sec travel speed
        public double SpotlightExpandSeconds { get; set; } = 1.0;  // final expand duration (must be > 0 to reach full B)
        public double SpotlightSoftEdge { get; set; } = 6.0;       // px feather

        // Jigsaw style (uses Divisions for the piece count across the width).
        public bool JigsawShowLines { get; set; } = true;          // draw piece-boundary lines while assembling
        public int JigsawLineR { get; set; } = 255;                // boundary line colour (RGB 0..255)
        public int JigsawLineG { get; set; } = 255;
        public int JigsawLineB { get; set; } = 255;

        public SpotlightParams ToSpotlightParams()
        {
            return new SpotlightParams
            {
                Radius = SpotlightRadius,
                Speed = SpotlightSpeed,
                ExpandSeconds = SpotlightExpandSeconds,
                Soft = SpotlightSoftEdge,
                Seed = Seed,
            };
        }

        // Brick (plank-drop) style. The overall pace is the shared MorphSeconds; these physics inputs shape
        // the fall acceleration and the impact bounce (height + g -> impact velocity -> bounce size;
        // hardness -> bounce amount; weight -> settle rate).
        public int BrickPieces { get; set; } = 10;                                  // planks along the fall axis
        public BrickDirection BrickDirection { get; set; } = BrickDirection.Down;
        public double BrickTotalHeightM { get; set; } = 5.0;                         // canvas height in metres
        public double BrickGravity { get; set; } = 9.8;                             // g (m/s^2)
        public double BrickWeight { get; set; } = 1.0;                              // heavier -> settles faster
        public double BrickHardness { get; set; } = 50.0;                           // 0..100 (%) -> bounce amount

        public BrickParams ToBrickParams()
        {
            return new BrickParams
            {
                Pieces = BrickPieces,
                Direction = BrickDirection,
                TotalHeightM = BrickTotalHeightM,
                Gravity = BrickGravity,
                Weight = BrickWeight,
                Hardness = BrickHardness / 100.0,
            };
        }
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
