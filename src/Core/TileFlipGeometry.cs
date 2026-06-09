using System;

namespace GifProcessorApp
{
    // A grid of near-square cells covering the canvas. Cols comes from the user's "divisions across the
    // width"; Rows is derived so each cell is as close to square as the canvas aspect allows. CellW/CellH
    // are the nominal (fractional) cell sizes; the renderer rounds per-cell edges so they tile exactly.
    public struct TileGrid
    {
        public int Cols, Rows;
        public double CellW, CellH;

        public int Count => Cols * Rows;
    }

    // Pure, dependency-free math for the tile-flip morph (linked into the test project; TileFlipRenderer
    // does the Magick crop/squash/composite). Each cell flips A->B over a short window staggered (in a
    // seeded scatter order) across the morph, so the picture assembles piece by piece. All cells are fully
    // flipped to B at t = 1.
    public static class TileFlipGeometry
    {
        // Split a w x h canvas into `divisions` columns and the row count that keeps cells ~square.
        public static TileGrid ComputeGrid(int w, int h, int divisions)
        {
            int cols = divisions < 1 ? 1 : divisions;
            double cellW = (double)Math.Max(1, w) / cols;
            int rows = (int)Math.Round(Math.Max(1, h) / cellW);
            if (rows < 1) rows = 1;
            double cellH = (double)Math.Max(1, h) / rows;
            return new TileGrid { Cols = cols, Rows = rows, CellW = cellW, CellH = cellH };
        }

        // Per-cell flip progress in [0,1] at morph progress t. Each cell starts at a seeded scatter offset
        // within [0, 1-flipFraction] and flips over flipFraction of the window; at t=1 every cell is 1.
        public static double CellPhase(int index, double t, int seed, double flipFraction)
        {
            double flip = flipFraction <= 0.0 ? 0.35 : flipFraction;
            if (flip > 1.0) flip = 1.0;
            double order = RainField.Hash01(index, 7, seed); // scatter order 0..1
            double start = order * (1.0 - flip);
            if (t <= start) return 0.0;
            double p = (t - start) / flip;
            return p >= 1.0 ? 1.0 : p;
        }

        // Squash factor on the flip axis: 1 at phase 0 (face A), 0 at phase 0.5 (edge-on), 1 at phase 1
        // (face B).
        public static double CellScale(double phase)
        {
            return Math.Abs(1.0 - 2.0 * phase);
        }

        // The cell shows clip B once it has passed the edge-on midpoint.
        public static bool CellShowsB(double phase)
        {
            return phase >= 0.5;
        }

        // Flip axis for a cell: 0 = horizontal squash (scaleX), 1 = vertical squash (scaleY). Up/Down flip
        // vertically, Left/Right horizontally, Random picks per-cell from the seeded hash.
        public static int CellAxis(int index, TileFlipDirection dir, int seed)
        {
            switch (dir)
            {
                case TileFlipDirection.Up:
                case TileFlipDirection.Down:
                    return 1;
                case TileFlipDirection.Left:
                case TileFlipDirection.Right:
                    return 0;
                default:
                    return RainField.Hash01(index, 8, seed) < 0.5 ? 0 : 1;
            }
        }
    }
}
