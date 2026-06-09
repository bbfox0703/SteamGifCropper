using System;
using GifProcessorApp;

namespace SteamGifCropper.Tests;

public class SpotlightFieldTests
{
    private static SpotlightParams Params() => new SpotlightParams
    {
        Radius = 80,
        Speed = 400,
        ExpandSeconds = 1.0,
        Soft = 6,
        Seed = 5,
    };

    [Fact]
    public void Bounce_StaysWithinRange()
    {
        for (double t = 0; t <= 10.0; t += 0.13)
        {
            double p = SpotlightField.Bounce(0.3, 250.0, t, 20.0, 180.0);
            Assert.InRange(p, 20.0, 180.0);
        }
    }

    [Fact]
    public void Center_StaysInsideCanvasBounds()
    {
        var p = Params();
        double r = SpotlightField.ClampRadius(p.Radius, 400, 300);
        for (double t = 0; t <= 1.0001; t += 0.05)
        {
            var (cx, cy) = SpotlightField.Center(t, 3.0, 400, 300, p);
            Assert.InRange(cx, r - 1e-6, 400 - r + 1e-6);
            Assert.InRange(cy, r - 1e-6, 300 - r + 1e-6);
        }
    }

    [Fact]
    public void Center_FrozenDuringExpandPhase()
    {
        var p = Params();
        double morph = 3.0;
        double expandFrac = SpotlightField.ExpandFrac(p.ExpandSeconds, morph);
        var atStart = SpotlightField.Center(expandFrac, morph, 400, 300, p);
        var later = SpotlightField.Center(1.0, morph, 400, 300, p);
        Assert.Equal(atStart.Cx, later.Cx, 6);
        Assert.Equal(atStart.Cy, later.Cy, 6);
    }

    [Fact]
    public void RadiusAt_GrowsToCoverWholeCanvasByEnd()
    {
        var p = Params();
        double morph = 3.0;
        double diag = Math.Sqrt(400.0 * 400.0 + 300.0 * 300.0);
        Assert.Equal(diag, SpotlightField.RadiusAt(1.0, morph, 400, 300, p), 3);
        // During the moving phase the radius is just the spotlight size.
        Assert.Equal(SpotlightField.ClampRadius(p.Radius, 400, 300),
            SpotlightField.RadiusAt(0.1, morph, 400, 300, p), 6);
    }

    [Fact]
    public void Coverage_FullInside_ZeroOutside()
    {
        Assert.Equal(1.0, SpotlightField.Coverage(100, 100, 100, 100, 40, 6), 6); // centre
        Assert.Equal(0.0, SpotlightField.Coverage(200, 200, 100, 100, 40, 6), 6); // far outside
    }

    [Fact]
    public void Coverage_AtEnd_IsFullEverywhere()
    {
        var p = Params();
        double morph = 3.0;
        var (cx, cy) = SpotlightField.Center(1.0, morph, 400, 300, p);
        double r = SpotlightField.RadiusAt(1.0, morph, 400, 300, p);
        // Every canvas corner is inside the final (diagonal) radius -> fully B.
        Assert.Equal(1.0, SpotlightField.Coverage(0, 0, cx, cy, r, p.Soft), 6);
        Assert.Equal(1.0, SpotlightField.Coverage(399, 299, cx, cy, r, p.Soft), 6);
    }
}
