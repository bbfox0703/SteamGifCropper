using System;
using System.Collections.Generic;

namespace GifProcessorApp
{
    // Pure, dependency-free grid geometry so it can be linked into the test project
    // (which uses a GifProcessor stub instead of the Magick-heavy real class).
    public static class GridMosaicGeometry
    {
        // Computes the inclusive pixel ranges of the (divisions-1) internal grid lines that
        // evenly divide a `span`-px region into `divisions` cells, each line `lineWidth` px wide.
        // Leftover pixels are distributed one-per-cell to the leading cells, so the result is
        // identical across equally-sized parts. Returns an empty list when divisions <= 1.
        public static List<(int Start, int End)> ComputeGridLineRanges(int span, int divisions, int lineWidth)
        {
            var lines = new List<(int Start, int End)>();
            if (divisions <= 1 || lineWidth <= 0)
            {
                return lines;
            }

            int totalLineWidth = (divisions - 1) * lineWidth;
            if (totalLineWidth >= span)
            {
                throw new ArgumentException($"Grid lines ({totalLineWidth}px) do not fit within span ({span}px).");
            }

            int cellSpace = span - totalLineWidth;
            int baseCell = cellSpace / divisions;
            int remainder = cellSpace % divisions;

            int pos = 0;
            for (int c = 0; c < divisions; c++)
            {
                int cellWidth = baseCell + (c < remainder ? 1 : 0);
                pos += cellWidth;
                if (c < divisions - 1)
                {
                    lines.Add((pos, pos + lineWidth - 1));
                    pos += lineWidth;
                }
            }

            return lines;
        }
    }
}
