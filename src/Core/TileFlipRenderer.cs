using System;
using ImageMagick;

namespace GifProcessorApp
{
    // Renders one tile-flip morph frame: for each grid cell, crop the cell from A or B (whichever face is
    // showing), squash it on the flip axis by the cell's current scale, and composite it centred in the
    // cell's rectangle. Cells not yet started show A at full size; finished cells show B at full size. The
    // grid math is the pure TileFlipGeometry; this class only does the Magick crop/resize/composite.
    public static class TileFlipRenderer
    {
        private const double FlipFraction = 0.35; // each cell's flip occupies 35% of the morph window

        // A and B must already be the same size. Returns a new canvas the size of A; the caller owns it.
        public static MagickImage RenderFrame(IMagickImage<byte> frameA, IMagickImage<byte> frameB,
            double t, TileGrid grid, MorphSettings settings)
        {
            int w = (int)frameA.Width;
            int h = (int)frameA.Height;
            var canvas = new MagickImage(MagickColors.Transparent, (uint)w, (uint)h);

            for (int r = 0; r < grid.Rows; r++)
            {
                int y0 = (int)Math.Round((double)r * h / grid.Rows);
                int y1 = (int)Math.Round((double)(r + 1) * h / grid.Rows);
                int ch = Math.Max(1, y1 - y0);

                for (int c = 0; c < grid.Cols; c++)
                {
                    int x0 = (int)Math.Round((double)c * w / grid.Cols);
                    int x1 = (int)Math.Round((double)(c + 1) * w / grid.Cols);
                    int cw = Math.Max(1, x1 - x0);

                    int index = r * grid.Cols + c;
                    double phase = TileFlipGeometry.CellPhase(index, t, settings.Seed, FlipFraction);
                    bool showB = TileFlipGeometry.CellShowsB(phase);
                    IMagickImage<byte> srcFrame = showB ? frameB : frameA;

                    using var cell = (MagickImage)srcFrame.Clone();
                    cell.Crop(new MagickGeometry(x0, y0, (uint)cw, (uint)ch));
                    cell.ResetPage();

                    double scale = TileFlipGeometry.CellScale(phase);
                    if (scale >= 0.999)
                    {
                        canvas.Composite(cell, x0, y0, CompositeOperator.Over);
                        continue;
                    }

                    int axis = TileFlipGeometry.CellAxis(index, settings.FlipDirection, settings.Seed);
                    int sw = axis == 0 ? Math.Max(1, (int)Math.Round(cw * scale)) : cw;
                    int sh = axis == 1 ? Math.Max(1, (int)Math.Round(ch * scale)) : ch;
                    cell.Resize(new MagickGeometry((uint)sw, (uint)sh) { IgnoreAspectRatio = true });

                    int cx = x0 + (cw - sw) / 2;
                    int cy = y0 + (ch - sh) / 2;
                    canvas.Composite(cell, cx, cy, CompositeOperator.Over);
                }
            }

            canvas.ResetPage();
            return canvas;
        }
    }
}
