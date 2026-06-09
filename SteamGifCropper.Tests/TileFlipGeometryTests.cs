using System;
using GifProcessorApp;

namespace SteamGifCropper.Tests;

public class TileFlipGeometryTests
{
    [Fact]
    public void ComputeGrid_SquareCanvas_GivesSquareCells()
    {
        var g = TileFlipGeometry.ComputeGrid(400, 400, 4);
        Assert.Equal(4, g.Cols);
        Assert.Equal(4, g.Rows);
        Assert.Equal(g.CellW, g.CellH, 6);
    }

    [Theory]
    [InlineData(766, 400, 8)]
    [InlineData(766, 766, 6)]
    [InlineData(300, 500, 5)]
    public void ComputeGrid_CellsAreNearSquare(int w, int h, int divisions)
    {
        var g = TileFlipGeometry.ComputeGrid(w, h, divisions);
        Assert.Equal(divisions, g.Cols);
        Assert.True(g.Rows >= 1);
        double ratio = g.CellW / g.CellH;
        // "near square": within a comfortable band given integer row rounding.
        Assert.InRange(ratio, 0.6, 1.6);
    }

    [Fact]
    public void CellScale_FlipsThroughZeroAtMidpoint()
    {
        Assert.Equal(1.0, TileFlipGeometry.CellScale(0.0), 6);
        Assert.Equal(0.0, TileFlipGeometry.CellScale(0.5), 6);
        Assert.Equal(1.0, TileFlipGeometry.CellScale(1.0), 6);
    }

    [Fact]
    public void CellShowsB_OnlyAfterMidpoint()
    {
        Assert.False(TileFlipGeometry.CellShowsB(0.0));
        Assert.False(TileFlipGeometry.CellShowsB(0.49));
        Assert.True(TileFlipGeometry.CellShowsB(0.5));
        Assert.True(TileFlipGeometry.CellShowsB(1.0));
    }

    [Fact]
    public void CellPhase_AllCellsZeroAtStart_FullAtEnd()
    {
        var g = TileFlipGeometry.ComputeGrid(766, 400, 8);
        for (int i = 0; i < g.Count; i++)
        {
            Assert.Equal(0.0, TileFlipGeometry.CellPhase(i, 0.0, 7, 0.35), 6);
            Assert.Equal(1.0, TileFlipGeometry.CellPhase(i, 1.0, 7, 0.35), 6);
        }
    }

    [Fact]
    public void CellAxis_FollowsDirection()
    {
        Assert.Equal(1, TileFlipGeometry.CellAxis(0, TileFlipDirection.Up, 7));
        Assert.Equal(1, TileFlipGeometry.CellAxis(3, TileFlipDirection.Down, 7));
        Assert.Equal(0, TileFlipGeometry.CellAxis(0, TileFlipDirection.Left, 7));
        Assert.Equal(0, TileFlipGeometry.CellAxis(3, TileFlipDirection.Right, 7));
    }
}
