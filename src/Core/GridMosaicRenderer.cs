using ImageMagick;

namespace GifProcessorApp
{
    // Renders the grid/lattice overlay for one Steam split part. Kept separate from the
    // Magick-heavy GifProcessor so it can be unit-tested directly (the test project links it).
    public static class GridMosaicRenderer
    {
        // Builds a transparent overlay the size of a split part, with grid lines drawn over the
        // image region only (the bottom extension stays transparent). For Transparent style the
        // lines are opaque white (used as a DstOut stencil to punch holes); for Solid style the
        // lines carry the chosen colour (composited Over).
        //
        // The lines are filled with direct pixel writes rather than ImageMagick Drawables: the
        // canvas already needs the "XC" coder (allowed in Program.cs), and pixel access keeps this
        // renderer from pulling in any additional coder/vector-drawing dependency. It runs once
        // per part (5x per GIF), so the per-pixel loop is not a hot path.
        public static MagickImage BuildGridLayer(uint partWidth, uint fullHeight, int regionHeight, GridMosaicSettings grid)
        {
            var layer = new MagickImage(MagickColors.Transparent, partWidth, fullHeight);

            var colLines = GridMosaicGeometry.ComputeGridLineRanges((int)partWidth, grid.ColumnsPerSlot, grid.LineWidth);
            var rowLines = GridMosaicGeometry.ComputeGridLineRanges(regionHeight, grid.Rows, grid.LineWidth);

            if (colLines.Count == 0 && rowLines.Count == 0)
            {
                return layer;
            }

            using (var pixels = layer.GetPixels())
            {
                int channels = (int)layer.ChannelCount;
                byte[] value = new byte[channels];
                if (grid.Style == GridLineStyle.Solid)
                {
                    if (channels > 0) value[0] = grid.LineColor.R;
                    if (channels > 1) value[1] = grid.LineColor.G;
                    if (channels > 2) value[2] = grid.LineColor.B;
                    if (channels > 3) value[3] = 255;
                }
                else
                {
                    // Opaque white stencil; only its alpha matters for the DstOut punch.
                    for (int c = 0; c < channels; c++)
                    {
                        value[c] = 255;
                    }
                }

                int width = (int)partWidth;
                foreach (var (start, end) in colLines)
                {
                    for (int x = start; x <= end; x++)
                    {
                        for (int y = 0; y < regionHeight; y++)
                        {
                            pixels.SetPixel(x, y, value);
                        }
                    }
                }
                foreach (var (start, end) in rowLines)
                {
                    for (int y = start; y <= end; y++)
                    {
                        for (int x = 0; x < width; x++)
                        {
                            pixels.SetPixel(x, y, value);
                        }
                    }
                }
            }

            return layer;
        }

        // Punches (Transparent) or draws (Solid) the grid onto one already-composed part frame.
        public static void ApplyGridLayer(IMagickImage<byte> frame, IMagickImage<byte> gridLayer, GridLineStyle style)
        {
            frame.Composite(gridLayer, 0, 0,
                style == GridLineStyle.Transparent ? CompositeOperator.DstOut : CompositeOperator.Over);
        }
    }
}
