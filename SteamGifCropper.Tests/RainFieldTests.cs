using System;
using GifProcessorApp;

namespace SteamGifCropper.Tests;

public class RainFieldTests
{
    [Fact]
    public void DropCount_ScalesWithAmount_AndClamps()
    {
        int low = RainField.DropCount(20, 766, 400);
        int high = RainField.DropCount(80, 766, 400);
        Assert.True(high > low);
        Assert.Equal(0, RainField.DropCount(0, 766, 400));
        // Negative / over-100 amounts are clamped (no exception, same as 0 / 100).
        Assert.Equal(RainField.DropCount(0, 766, 400), RainField.DropCount(-50, 766, 400));
        Assert.Equal(RainField.DropCount(100, 766, 400), RainField.DropCount(150, 766, 400));
    }

    [Fact]
    public void Streaks_ReturnsRequestedCount()
    {
        var p = new RainParams { Count = 50, FallSpeed = 400, WindX = 0, StreakLength = 20, Seed = 123 };
        var streaks = RainField.Streaks(0.0, 766, 400, p);
        Assert.Equal(50, streaks.Length);
    }

    [Fact]
    public void Streaks_TailTrailsBehindVelocity()
    {
        // Wind to the right + gravity: the tail is up and to the LEFT of the head (X1<X0, Y1<Y0).
        var right = new RainParams { Count = 40, FallSpeed = 400, WindX = 200, StreakLength = 24, Seed = 7 };
        foreach (var s in RainField.Streaks(0.0, 766, 400, right))
        {
            Assert.True(s.X1 <= s.X0 + 1e-9);
            Assert.True(s.Y1 <= s.Y0 + 1e-9);
        }

        // Wind to the left: the tail is to the RIGHT of the head (X1>X0).
        var left = new RainParams { Count = 40, FallSpeed = 400, WindX = -200, StreakLength = 24, Seed = 7 };
        foreach (var s in RainField.Streaks(0.0, 766, 400, left))
        {
            Assert.True(s.X1 >= s.X0 - 1e-9);
        }
    }

    [Fact]
    public void ToParams_WindDirectionSetsSign()
    {
        Assert.True(new RainSettings { WindDirection = RainWindDirection.Left, WindStrength = 150 }.ToParams(766, 400).WindX < 0);
        Assert.True(new RainSettings { WindDirection = RainWindDirection.Right, WindStrength = 150 }.ToParams(766, 400).WindX > 0);
        Assert.Equal(0.0, new RainSettings { WindDirection = RainWindDirection.None, WindStrength = 150 }.ToParams(766, 400).WindX, 6);
    }

    [Fact]
    public void FadeAlpha_FullInsideWindow_ZeroOutside()
    {
        Assert.Equal(1.0, RainField.FadeAlpha(2.0, 6.0, false, 1.0), 6);
        Assert.Equal(0.0, RainField.FadeAlpha(-0.1, 6.0, false, 1.0), 6);
        Assert.Equal(0.0, RainField.FadeAlpha(6.1, 6.0, false, 1.0), 6);
    }

    [Fact]
    public void FadeAlpha_RampsToZeroOverFadeWindow()
    {
        // 6s window, 2s fade-out -> full until t=4, half at t=5, zero at t=6.
        Assert.Equal(1.0, RainField.FadeAlpha(3.9, 6.0, true, 2.0), 6);
        Assert.Equal(0.5, RainField.FadeAlpha(5.0, 6.0, true, 2.0), 6);
        Assert.Equal(0.0, RainField.FadeAlpha(6.0, 6.0, true, 2.0), 6);
        Assert.False(RainField.AnyRainActive(6.0, 6.0, true, 2.0));
        Assert.True(RainField.AnyRainActive(3.0, 6.0, true, 2.0));
    }
}
