using System;
using System.Linq;
using GifProcessorApp;

namespace SteamGifCropper.Tests;

public class BrickFieldTests
{
    private static BrickParams Params(BrickDirection dir = BrickDirection.Down, int pieces = 10) => new BrickParams
    {
        Pieces = pieces,
        Direction = dir,
        TotalHeightM = 5.0,
        Gravity = 9.8,
        Weight = 1.0,
        Hardness = 0.5,
    };

    [Fact]
    public void Planks_AtEnd_AllLandedAtTheirSlot()
    {
        var planks = BrickField.Planks(1.0, Params(), 400);
        Assert.Equal(10, planks.Length);
        foreach (var pl in planks)
        {
            Assert.True(pl.Started);
            Assert.Equal(pl.SliceStart, pl.CurrentPos); // at rest -> full B
        }
    }

    [Fact]
    public void Planks_SlicesTileTheAxis()
    {
        int L = 400;
        var planks = BrickField.Planks(1.0, Params(pieces: 7), L);
        Assert.Equal(L, planks.Sum(p => p.SliceLen));
        Assert.Equal(0, planks.Min(p => p.SliceStart));
        Assert.Equal(L, planks.Max(p => p.SliceStart + p.SliceLen));
    }

    [Fact]
    public void Planks_AtStart_FirstIsFallingOffScreen_RestNotStarted()
    {
        var planks = BrickField.Planks(0.0, Params(), 400);
        Assert.True(planks[0].Started);
        Assert.True(planks[0].CurrentPos < 0); // Down starts just above the top edge
        Assert.False(planks[9].Started);
    }

    [Fact]
    public void Planks_DropOrderStaggered_MoreStartedOverTime()
    {
        int early = BrickField.Planks(0.1, Params(), 400).Count(p => p.Started);
        int late = BrickField.Planks(0.6, Params(), 400).Count(p => p.Started);
        Assert.True(late > early);
    }

    [Fact]
    public void Direction_DeterminesWhichSliceDropsFirst()
    {
        // Down stacks from the bottom: drop order 0 fills the bottom slice (largest SliceStart).
        var down = BrickField.Planks(1.0, Params(BrickDirection.Down), 400);
        Assert.Equal(down.Max(p => p.SliceStart), down[0].SliceStart);
        // Up stacks from the top: drop order 0 fills the top slice (SliceStart 0).
        var up = BrickField.Planks(1.0, Params(BrickDirection.Up), 400);
        Assert.Equal(0, up[0].SliceStart);
    }

    [Fact]
    public void IsVertical_MatchesDirection()
    {
        Assert.True(BrickField.IsVertical(BrickDirection.Down));
        Assert.True(BrickField.IsVertical(BrickDirection.Up));
        Assert.False(BrickField.IsVertical(BrickDirection.Left));
        Assert.False(BrickField.IsVertical(BrickDirection.Right));
    }
}
