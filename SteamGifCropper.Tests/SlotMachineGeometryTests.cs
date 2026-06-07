using System;
using GifProcessorApp;

namespace SteamGifCropper.Tests;

public class SlotMachineGeometryTests
{
    private const int CanvasHeight = 400;

    [Theory]
    [InlineData(-0.5, 0.0)]
    [InlineData(0.0, 0.0)]
    [InlineData(1.0, 1.0)]
    [InlineData(2.0, 1.0)]
    public void EaseOutCubic_IsClampedToUnitInterval(double t, double expected)
    {
        Assert.Equal(expected, SlotMachineGeometry.EaseOutCubic(t), 6);
    }

    [Fact]
    public void EaseOutCubic_IsMonotonicIncreasing()
    {
        double prev = -1.0;
        for (int i = 0; i <= 20; i++)
        {
            double v = SlotMachineGeometry.EaseOutCubic(i / 20.0);
            Assert.True(v >= prev, "ease-out cubic must be non-decreasing");
            Assert.InRange(v, 0.0, 1.0);
            prev = v;
        }
    }

    [Fact]
    public void ApplyVariance_MidpointLeavesValueUnchanged()
    {
        Assert.Equal(10.0, SlotMachineGeometry.ApplyVariance(10.0, 30.0, 0.5), 6);
    }

    [Fact]
    public void ApplyVariance_EndpointsHitFullSwing()
    {
        Assert.Equal(13.0, SlotMachineGeometry.ApplyVariance(10.0, 30.0, 1.0), 6); // +30%
        Assert.Equal(7.0, SlotMachineGeometry.ApplyVariance(10.0, 30.0, 0.0), 6);  // -30%
    }

    [Fact]
    public void ApplyVariance_ZeroVarianceIsIdentity()
    {
        Assert.Equal(4.0, SlotMachineGeometry.ApplyVariance(4.0, 0.0, 0.9), 6);
    }

    [Theory]
    [InlineData(-1.0)]
    [InlineData(2.0)]
    public void ApplyVariance_ClampsRandToUnitInterval(double rand01)
    {
        double v = SlotMachineGeometry.ApplyVariance(10.0, 50.0, rand01);
        Assert.InRange(v, 5.0, 15.0);
    }

    [Fact]
    public void ReelOffsetY_IsAlwaysWithinCanvasHeight()
    {
        const int stop = 45;
        const int overshoot = 8;
        for (int f = 0; f < stop + overshoot + 10; f++)
        {
            int off = SlotMachineGeometry.ReelOffsetY(f, stop, 4, CanvasHeight, true, overshoot);
            Assert.InRange(off, 0, CanvasHeight - 1);
        }
    }

    [Fact]
    public void ReelOffsetY_LocksToZeroAfterSettleFrame()
    {
        const int stop = 45;
        const int overshoot = 8;
        int settle = stop + overshoot;
        Assert.Equal(0, SlotMachineGeometry.ReelOffsetY(settle, stop, 4, CanvasHeight, true, overshoot));
        Assert.Equal(0, SlotMachineGeometry.ReelOffsetY(settle + 5, stop, 4, CanvasHeight, true, overshoot));
    }

    [Fact]
    public void ReelOffsetY_IsZeroAtBounceStart()
    {
        const int stop = 45;
        Assert.Equal(0, SlotMachineGeometry.ReelOffsetY(stop, stop, 4, CanvasHeight, true, 8));
    }

    [Fact]
    public void ReelOffsetY_NoOvershoot_LocksAtStopFrame()
    {
        const int stop = 30;
        Assert.Equal(0, SlotMachineGeometry.ReelOffsetY(stop, stop, 3, CanvasHeight, true, 0));
        Assert.Equal(0, SlotMachineGeometry.ReelOffsetY(stop + 3, stop, 3, CanvasHeight, true, 0));
    }

    [Fact]
    public void ReelOffsetY_DirectionsAreComplementary()
    {
        const int stop = 50;
        for (int f = 0; f < stop; f++)
        {
            int down = SlotMachineGeometry.ReelOffsetY(f, stop, 4, CanvasHeight, true, 0);
            int up = SlotMachineGeometry.ReelOffsetY(f, stop, 4, CanvasHeight, false, 0);
            Assert.Equal((CanvasHeight - down) % CanvasHeight, up);
        }
    }

    [Fact]
    public void ReelOffsetY_ReturnsZeroForNonPositiveCanvas()
    {
        Assert.Equal(0, SlotMachineGeometry.ReelOffsetY(3, 40, 4, 0, true, 5));
    }
}
