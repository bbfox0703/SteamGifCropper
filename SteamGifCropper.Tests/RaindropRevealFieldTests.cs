using System;
using GifProcessorApp;

namespace SteamGifCropper.Tests;

public class RaindropRevealFieldTests
{
    private static MorphSettings Settings() => new MorphSettings
    {
        Seed = 42,
        RainIntensity = 20,
        SpreadRadius = 80,
        DropSizeVariationPct = 40,
        SoftEdge = 8,
    };

    [Fact]
    public void BuildDrops_ReturnsRequestedCount_AndIsDeterministic()
    {
        var s = Settings();
        var a = RaindropRevealField.BuildDrops(s, 300, 200);
        var b = RaindropRevealField.BuildDrops(s, 300, 200);
        Assert.Equal(20, a.Length);
        Assert.Equal(a[0].Px, b[0].Px, 9);
        Assert.Equal(a[5].MaxR, b[5].MaxR, 9);
    }

    [Fact]
    public void Coverage_IsZeroAtStart_AndFullAtEnd()
    {
        var drops = RaindropRevealField.BuildDrops(Settings(), 300, 200);
        Assert.Equal(0.0, RaindropRevealField.Coverage(150, 100, 0.0, drops, 8), 6);
        // Global floor guarantees a fully-B frame at t=1 everywhere on the canvas.
        Assert.Equal(1.0, RaindropRevealField.Coverage(10, 10, 1.0, drops, 8), 6);
        Assert.Equal(1.0, RaindropRevealField.Coverage(299, 199, 1.0, drops, 8), 6);
    }

    [Fact]
    public void Coverage_IsMonotonicInTime()
    {
        var drops = RaindropRevealField.BuildDrops(Settings(), 300, 200);
        double prev = -1.0;
        for (double t = 0.0; t <= 1.0001; t += 0.1)
        {
            double c = RaindropRevealField.Coverage(150, 100, t, drops, 8);
            Assert.True(c >= prev - 1e-9, $"coverage dropped at t={t}: {c} < {prev}");
            prev = c;
        }
    }
}
