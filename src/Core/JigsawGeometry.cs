using System;

namespace GifProcessorApp
{
    // Pure, dependency-free math for the jigsaw morph (linked into the test project; JigsawRenderer does
    // the Magick blend + line drawing, and reuses TileFlipGeometry.ComputeGrid for the near-square grid).
    // Pieces are "placed" one by one in a seeded scatter order across the window — each placed piece
    // cross-fades A->B over a short fill window. Boundary lines are visible while assembling and fade out
    // at the very end, so the finished frame is plain full B.
    public static class JigsawGeometry
    {
        // Placement progress of piece `index` at morph progress t (in [0,1]): 0 = not yet placed (clip A),
        // 1 = fully placed (clip B). Each piece starts at a seeded scatter offset and fills over
        // fillFraction of the window; every piece is 1 at t = 1.
        public static double PiecePhase(int index, double t, int seed, double fillFraction)
        {
            double fill = fillFraction <= 0.0 ? 0.12 : fillFraction;
            if (fill > 1.0) fill = 1.0;
            double order = RainField.Hash01(index, 21, seed); // jigsaw-specific salt (differs from tile flip)
            double start = order * (1.0 - fill);
            if (t <= start) return 0.0;
            double p = (t - start) / fill;
            return p >= 1.0 ? 1.0 : p;
        }

        // Opacity of the piece-boundary lines at t: fully visible until fadeStart, then linearly 1 -> 0 by
        // t = 1 (so the assembled picture is left clean, with no grid).
        public static double LineAlpha(double t, double fadeStart)
        {
            double fs = fadeStart;
            if (fs < 0.0) fs = 0.0;
            if (fs > 1.0) fs = 1.0;
            if (t <= fs) return 1.0;
            double a = 1.0 - (t - fs) / Math.Max(1e-6, 1.0 - fs);
            return a < 0.0 ? 0.0 : a;
        }
    }
}
