using System;
using ImageMagick;
using GifProcessorApp;

namespace SteamGifCropper.Tests;

// Runtime smoke tests for the Magick read/write/crop/resize/composite paths of the rain + morph
// renderers (the geometry/coverage math is covered by the *FieldTests / *GeometryTests). Each verifies
// RenderFrame produces a valid same-size image without throwing.
public class MorphRainRendererTests
{
    private static MorphSettings RaindropSettings() => new MorphSettings
    {
        Style = MorphStyle.RaindropReveal,
        Seed = 7,
        RainIntensity = 12,
        SpreadRadius = 30,
        DropSizeVariationPct = 40,
        SoftEdge = 6,
    };

    [Fact]
    public void RainRenderer_PreservesDimensions()
    {
        using var src = new MagickImage(MagickColors.SteelBlue, 64, 48);
        var p = new RainParams { Count = 60, FallSpeed = 200, WindX = 80, StreakLength = 16, Seed = 3 };
        var streaks = RainField.Streaks(0.5, 64, 48, p);
        using var outImg = RainRenderer.RenderFrame(src, streaks, 0.8);
        Assert.Equal(64u, outImg.Width);
        Assert.Equal(48u, outImg.Height);
    }

    [Fact]
    public void RainRenderer_ZeroAlphaOrNoStreaks_DoesNotThrow()
    {
        using var src = new MagickImage(MagickColors.SteelBlue, 32, 32);
        using var a = RainRenderer.RenderFrame(src, new RainStreak[0], 1.0);
        using var b = RainRenderer.RenderFrame(src, RainField.Streaks(0.0, 32, 32, new RainParams { Count = 10, FallSpeed = 100, StreakLength = 8, Seed = 1 }), 0.0);
        Assert.Equal(32u, a.Width);
        Assert.Equal(32u, b.Width);
    }

    [Fact]
    public void RaindropRevealRenderer_PreservesDimensions_AcrossProgress()
    {
        using var a = new MagickImage(MagickColors.Red, 50, 40);
        using var b = new MagickImage(MagickColors.Blue, 50, 40);
        var drops = RaindropRevealField.BuildDrops(RaindropSettings(), 50, 40);
        foreach (double t in new[] { 0.0, 0.5, 1.0 })
        {
            using var outImg = RaindropRevealRenderer.RenderFrame(a, b, t, drops, 6);
            Assert.Equal(50u, outImg.Width);
            Assert.Equal(40u, outImg.Height);
        }
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(0.5)]
    [InlineData(1.0)]
    public void TileFlipRenderer_PreservesDimensions(double t)
    {
        using var a = new MagickImage(MagickColors.Red, 80, 50);
        using var b = new MagickImage(MagickColors.Blue, 80, 50);
        var settings = new MorphSettings { Style = MorphStyle.TileFlip, Divisions = 6, FlipDirection = TileFlipDirection.Random, Seed = 9 };
        var grid = TileFlipGeometry.ComputeGrid(80, 50, settings.Divisions);
        using var outImg = TileFlipRenderer.RenderFrame(a, b, t, grid, settings);
        Assert.Equal(80u, outImg.Width);
        Assert.Equal(50u, outImg.Height);
    }

    [Fact]
    public void MorphFrame_NextToClonedFrame_OptimizeDoesNotThrow()
    {
        // The morph engine mixes rendered morph frames with cloned A/B frames; without uniform page
        // metadata Optimize() raises "image pages are not coalesced". The renderer ResetPage()s its
        // output and the engine ResetPage()s the clones — together they must Optimize cleanly.
        using var a = new MagickImage(MagickColors.Red, 60, 40);
        using var b = new MagickImage(MagickColors.Blue, 60, 40);
        var settings = new MorphSettings { Style = MorphStyle.TileFlip, Divisions = 4, Seed = 2 };
        var grid = TileFlipGeometry.ComputeGrid(60, 40, settings.Divisions);
        using var coll = new MagickImageCollection();

        var rendered = TileFlipRenderer.RenderFrame(a, b, 0.5, grid, settings);
        rendered.AnimationDelay = 5;
        coll.Add(rendered);

        var cloned = (MagickImage)b.Clone();
        cloned.ResetPage();
        cloned.AnimationDelay = 5;
        coll.Add(cloned);

        coll.Optimize(); // must not throw
        Assert.Equal(2, coll.Count);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(0.5)]
    [InlineData(1.0)]
    public void SpotlightRenderer_PreservesDimensions(double t)
    {
        using var a = new MagickImage(MagickColors.Red, 64, 48);
        using var b = new MagickImage(MagickColors.Blue, 64, 48);
        var p = new SpotlightParams { Radius = 20, Speed = 300, ExpandSeconds = 1.0, Soft = 6, Seed = 4 };
        using var outImg = SpotlightRenderer.RenderFrame(a, b, t, 3.0, p);
        Assert.Equal(64u, outImg.Width);
        Assert.Equal(48u, outImg.Height);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(0.5)]
    [InlineData(1.0)]
    public void JigsawRenderer_PreservesDimensions(double t)
    {
        using var a = new MagickImage(MagickColors.Red, 80, 50);
        using var b = new MagickImage(MagickColors.Blue, 80, 50);
        var settings = new MorphSettings { Style = MorphStyle.Jigsaw, Divisions = 6, Seed = 9, JigsawShowLines = true, JigsawLineR = 255, JigsawLineG = 255, JigsawLineB = 255 };
        var grid = TileFlipGeometry.ComputeGrid(80, 50, settings.Divisions);
        using var outImg = JigsawRenderer.RenderFrame(a, b, t, grid, settings);
        Assert.Equal(80u, outImg.Width);
        Assert.Equal(50u, outImg.Height);
    }

    [Theory]
    [InlineData(BrickDirection.Down, 0.0)]
    [InlineData(BrickDirection.Down, 0.5)]
    [InlineData(BrickDirection.Down, 1.0)]
    [InlineData(BrickDirection.Up, 0.5)]
    [InlineData(BrickDirection.Left, 0.5)]
    [InlineData(BrickDirection.Right, 0.5)]
    public void BrickRenderer_PreservesDimensions(BrickDirection dir, double t)
    {
        using var a = new MagickImage(MagickColors.Red, 80, 50);
        using var b = new MagickImage(MagickColors.Blue, 80, 50);
        var p = new BrickParams { Pieces = 8, Direction = dir, TotalHeightM = 5, Gravity = 9.8, Weight = 1, Hardness = 0.5 };
        using var outImg = BrickRenderer.RenderFrame(a, b, t, p);
        Assert.Equal(80u, outImg.Width);
        Assert.Equal(50u, outImg.Height);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(0.5)]
    [InlineData(1.0)]
    public void WaterRenderer_PreservesDimensions(double fill)
    {
        using var a = new MagickImage(MagickColors.Red, 64, 48);
        using var b = new MagickImage(MagickColors.Blue, 64, 48);
        var p = new WaterParams { Direction = WaterDirection.Up, RefractionStrength = 4, SurfaceWobble = 6, BubbleSize = 10, Layers = 3, Seed = 3 };
        using var outImg = WaterRenderer.RenderFrame(a, b, fill, 1.2, p, 0.7);
        Assert.Equal(64u, outImg.Width);
        Assert.Equal(48u, outImg.Height);
    }
}
