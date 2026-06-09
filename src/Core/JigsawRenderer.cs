using System;
using System.Threading.Tasks;
using ImageMagick;

namespace GifProcessorApp
{
    // Renders one jigsaw morph frame: clip A is the base; pieces fill in (cross-fade A->B) one by one in a
    // seeded scatter order, and the piece-boundary lines (optional colour) are drawn over the top while
    // assembling and fade out by the end. Grid is the shared near-square TileFlipGeometry.ComputeGrid; the
    // per-piece timing + line fade are JigsawGeometry. Pixels/lines are written into a raw RGBA byte[].
    public static class JigsawRenderer
    {
        private const double FillFraction = 0.12;  // each piece cross-fades over 12% of the window
        private const double LineFadeStart = 0.85; // boundary lines start fading out at 85% of the window
        private const double LineOpacity = 0.85;   // peak line opacity

        public static MagickImage RenderFrame(IMagickImage<byte> frameA, IMagickImage<byte> frameB,
            double t, TileGrid grid, MorphSettings settings)
        {
            uint w = frameA.Width;
            uint h = frameA.Height;
            int iw = (int)w;
            int ih = (int)h;
            int cols = Math.Max(1, grid.Cols);
            int rows = Math.Max(1, grid.Rows);

            byte[] a, b;
            using (var sp = frameA.GetPixels()) a = sp.ToByteArray("RGBA")!;
            using (var sp = frameB.GetPixels()) b = sp.ToByteArray("RGBA")!;
            byte[] dst = new byte[a.Length];

            // Integer piece edges (same rounding as TileFlipRenderer) + per-pixel piece lookups.
            int[] colEdge = new int[cols + 1];
            for (int c = 0; c <= cols; c++) colEdge[c] = (int)Math.Round((double)c * iw / cols);
            int[] rowEdge = new int[rows + 1];
            for (int r = 0; r <= rows; r++) rowEdge[r] = (int)Math.Round((double)r * ih / rows);

            int[] colOf = BuildLookup(colEdge, iw, cols);
            int[] rowOf = BuildLookup(rowEdge, ih, rows);

            int seed = settings.Seed;
            Parallel.For(0, ih, y =>
            {
                int row = rowOf[y];
                int rowBase = y * iw * 4;
                for (int x = 0; x < iw; x++)
                {
                    int index = row * cols + colOf[x];
                    double cov = JigsawGeometry.PiecePhase(index, t, seed, FillFraction);
                    double inv = 1.0 - cov;
                    int idx = rowBase + x * 4;
                    for (int ch = 0; ch < 4; ch++)
                    {
                        double v = a[idx + ch] * inv + b[idx + ch] * cov;
                        int iv = (int)(v + 0.5);
                        dst[idx + ch] = iv < 0 ? (byte)0 : iv > 255 ? (byte)255 : (byte)iv;
                    }
                }
            });

            // Boundary lines (internal edges only) over the top, fading out at the end.
            if (settings.JigsawShowLines)
            {
                double lineAlpha = LineOpacity * JigsawGeometry.LineAlpha(t, LineFadeStart);
                if (lineAlpha > 0.0)
                {
                    byte lr = (byte)Math.Clamp(settings.JigsawLineR, 0, 255);
                    byte lg = (byte)Math.Clamp(settings.JigsawLineG, 0, 255);
                    byte lb = (byte)Math.Clamp(settings.JigsawLineB, 0, 255);
                    for (int c = 1; c < cols; c++)
                    {
                        int ex = colEdge[c]; if (ex >= iw) ex = iw - 1;
                        for (int y = 0; y < ih; y++) BlendPixel(dst, (y * iw + ex) * 4, lr, lg, lb, lineAlpha);
                    }
                    for (int r = 1; r < rows; r++)
                    {
                        int ey = rowEdge[r]; if (ey >= ih) ey = ih - 1;
                        int rowBase = ey * iw * 4;
                        for (int x = 0; x < iw; x++) BlendPixel(dst, rowBase + x * 4, lr, lg, lb, lineAlpha);
                    }
                }
            }

            var pixelSettings = new PixelReadSettings(w, h, StorageType.Char, "RGBA");
            var outImg = new MagickImage();
            outImg.ReadPixels(dst, pixelSettings);
            outImg.ResetPage();
            return outImg;
        }

        // Map each pixel coordinate to its piece column/row index from the integer edges.
        private static int[] BuildLookup(int[] edges, int size, int count)
        {
            int[] lookup = new int[size];
            int seg = 0;
            for (int i = 0; i < size; i++)
            {
                while (seg < count - 1 && i >= edges[seg + 1]) seg++;
                lookup[i] = seg;
            }
            return lookup;
        }

        private static void BlendPixel(byte[] buf, int idx, byte r, byte g, byte bb, double a)
        {
            double inv = 1.0 - a;
            buf[idx] = (byte)(buf[idx] * inv + r * a + 0.5);
            buf[idx + 1] = (byte)(buf[idx + 1] * inv + g * a + 0.5);
            buf[idx + 2] = (byte)(buf[idx + 2] * inv + bb * a + 0.5);
            int na = (int)(buf[idx + 3] + a * 255.0 + 0.5);
            buf[idx + 3] = na > 255 ? (byte)255 : (byte)na;
        }
    }
}
