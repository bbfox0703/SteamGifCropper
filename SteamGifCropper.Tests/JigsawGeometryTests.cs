using System;
using GifProcessorApp;

namespace SteamGifCropper.Tests;

public class JigsawGeometryTests
{
    [Fact]
    public void PiecePhase_AllZeroAtStart_AllOneAtEnd()
    {
        var grid = TileFlipGeometry.ComputeGrid(766, 400, 8);
        for (int i = 0; i < grid.Count; i++)
        {
            Assert.Equal(0.0, JigsawGeometry.PiecePhase(i, 0.0, 3, 0.12), 6);
            Assert.Equal(1.0, JigsawGeometry.PiecePhase(i, 1.0, 3, 0.12), 6);
        }
    }

    [Fact]
    public void PiecePhase_IsMonotonicInTime()
    {
        double prev = -1.0;
        for (double t = 0.0; t <= 1.0001; t += 0.1)
        {
            double p = JigsawGeometry.PiecePhase(17, t, 3, 0.12);
            Assert.True(p >= prev - 1e-9, $"piece phase dropped at t={t}");
            prev = p;
        }
    }

    [Fact]
    public void LineAlpha_VisibleEarly_GoneAtEnd()
    {
        Assert.Equal(1.0, JigsawGeometry.LineAlpha(0.0, 0.85), 6);
        Assert.Equal(1.0, JigsawGeometry.LineAlpha(0.85, 0.85), 6);
        Assert.Equal(0.0, JigsawGeometry.LineAlpha(1.0, 0.85), 6);
        Assert.InRange(JigsawGeometry.LineAlpha(0.925, 0.85), 0.4, 0.6); // halfway through the fade
    }
}
