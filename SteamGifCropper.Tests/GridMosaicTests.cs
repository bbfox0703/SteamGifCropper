using System;
using System.Collections.Generic;
using System.Linq;
using GifProcessorApp;
using ImageMagick;

namespace SteamGifCropper.Tests;

public class GridMosaicTests
{
    [Fact]
    public void ComputeGridLineRanges_FourColumns_ProducesThreeLinesThatTile()
    {
        var lines = GridMosaicGeometry.ComputeGridLineRanges(150, 4, 4);

        Assert.Equal(3, lines.Count);
        foreach (var (start, end) in lines)
        {
            Assert.Equal(4, end - start + 1);   // each line is exactly lineWidth px
            Assert.InRange(start, 0, 149);
            Assert.InRange(end, 0, 149);
        }
        for (int i = 1; i < lines.Count; i++)
        {
            Assert.True(lines[i].Start > lines[i - 1].End, "lines must be strictly increasing and non-overlapping");
        }
        Assert.Equal(150, TotalCovered(lines, 150));   // cells + lines exactly fill the span, no gaps
    }

    [Fact]
    public void ComputeGridLineRanges_SingleDivision_ReturnsNoLines()
    {
        Assert.Empty(GridMosaicGeometry.ComputeGridLineRanges(150, 1, 4));
    }

    [Fact]
    public void ComputeGridLineRanges_DistributesRemainderToLeadingCells()
    {
        // span=100, 5 cells, 2px lines: 4 lines = 8px, 92px of cells => base 18, remainder 2.
        var lines = GridMosaicGeometry.ComputeGridLineRanges(100, 5, 2);

        Assert.Equal(4, lines.Count);
        Assert.All(lines, l => Assert.Equal(2, l.End - l.Start + 1));
        Assert.Equal(100, TotalCovered(lines, 100));
        Assert.Equal(new[] { 19, 19, 18, 18, 18 }, CellWidths(lines, 100));
    }

    [Fact]
    public void ComputeGridLineRanges_LinesTooWide_Throws()
    {
        // (5-1)*40 = 160 >= 150 -> cannot fit
        Assert.Throws<ArgumentException>(() => GridMosaicGeometry.ComputeGridLineRanges(150, 5, 40));
    }

    [Fact]
    public void BuildGridLayer_Transparent_PunchesHolesAtLinesOnly()
    {
        var grid = new GridMosaicSettings
        {
            ColumnsPerSlot = 2,
            Rows = 1,
            LineWidth = 2,
            Style = GridLineStyle.Transparent
        };

        // partWidth 20, fullHeight 15, image region 10 -> one vertical line at x=9..10 over rows 0..9.
        using var layer = GridMosaicRenderer.BuildGridLayer(20, 15, 10, grid);
        using var frame = new MagickImage(MagickColors.Red, 20, 15);
        frame.HasAlpha = true;

        GridMosaicRenderer.ApplyGridLayer(frame, layer, grid.Style);

        using var pixels = frame.GetPixels();
        Assert.Equal(0, (int)pixels.GetPixel(9, 5).ToColor()!.A);    // line punched transparent
        Assert.Equal(0, (int)pixels.GetPixel(10, 5).ToColor()!.A);
        Assert.Equal(255, (int)pixels.GetPixel(0, 5).ToColor()!.A);  // cell stays opaque
        Assert.Equal(255, (int)pixels.GetPixel(9, 12).ToColor()!.A); // extension band (y>=10) untouched
    }

    [Fact]
    public void BuildGridLayer_Solid_DrawsColouredLines()
    {
        var grid = new GridMosaicSettings
        {
            ColumnsPerSlot = 2,
            Rows = 1,
            LineWidth = 2,
            Style = GridLineStyle.Solid,
            LineColor = System.Drawing.Color.Lime
        };

        using var layer = GridMosaicRenderer.BuildGridLayer(20, 15, 10, grid);
        using var frame = new MagickImage(MagickColors.Red, 20, 15);

        GridMosaicRenderer.ApplyGridLayer(frame, layer, grid.Style);

        using var pixels = frame.GetPixels();
        var line = pixels.GetPixel(9, 5).ToColor()!;
        Assert.Equal(0, (int)line.R);
        Assert.Equal(255, (int)line.G);
        Assert.Equal(0, (int)line.B);

        var cell = pixels.GetPixel(0, 5).ToColor()!;
        Assert.Equal(255, (int)cell.R);
        Assert.Equal(0, (int)cell.G);
    }

    // Sum of every cell width plus every line width; must equal the span with no gaps left over.
    private static int TotalCovered(List<(int Start, int End)> lines, int span)
    {
        int covered = 0;
        int pos = 0;
        foreach (var (start, end) in lines)
        {
            covered += start - pos;       // cell preceding this line
            covered += end - start + 1;   // the line itself
            pos = end + 1;
        }
        covered += span - pos;            // trailing cell
        return covered;
    }

    private static int[] CellWidths(List<(int Start, int End)> lines, int span)
    {
        var widths = new List<int>();
        int pos = 0;
        foreach (var (start, end) in lines)
        {
            widths.Add(start - pos);
            pos = end + 1;
        }
        widths.Add(span - pos);
        return widths.ToArray();
    }
}
