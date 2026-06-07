using System;
using GifProcessorApp;

namespace SteamGifCropper.Tests;

public class SlotMachineGeometryTests
{
    private const int ReelCount = 5;
    private const int TotalSpinFrames = 60;
    private const int StaggerFrames = 6;
    private const int CanvasHeight = 400;
    private const int Spins = 4;

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
    public void ReelStopFrame_RightmostStopsLast_LeftmostStopsFirst()
    {
        int prev = 0;
        for (int c = 0; c < ReelCount; c++)
        {
            int stop = SlotMachineGeometry.ReelStopFrame(c, ReelCount, TotalSpinFrames, StaggerFrames);
            Assert.True(stop > prev, "later reels must stop strictly later");
            prev = stop;
        }
        // Rightmost reel stops exactly at the end of the spin phase.
        Assert.Equal(TotalSpinFrames, SlotMachineGeometry.ReelStopFrame(ReelCount - 1, ReelCount, TotalSpinFrames, StaggerFrames));
    }

    [Fact]
    public void ReelStopFrame_NeverBelowOne_EvenWithHugeStagger()
    {
        int stop = SlotMachineGeometry.ReelStopFrame(0, ReelCount, 10, 100);
        Assert.True(stop >= 1);
    }

    [Fact]
    public void ReelOffsetY_IsAlwaysWithinCanvasHeight()
    {
        for (int c = 0; c < ReelCount; c++)
        {
            for (int f = 0; f < TotalSpinFrames + 20; f++)
            {
                int off = SlotMachineGeometry.ReelOffsetY(f, c, ReelCount, TotalSpinFrames, StaggerFrames, CanvasHeight, Spins);
                Assert.InRange(off, 0, CanvasHeight - 1);
            }
        }
    }

    [Fact]
    public void ReelOffsetY_LocksToZeroAtAndAfterStopFrame()
    {
        for (int c = 0; c < ReelCount; c++)
        {
            int stop = SlotMachineGeometry.ReelStopFrame(c, ReelCount, TotalSpinFrames, StaggerFrames);
            Assert.Equal(0, SlotMachineGeometry.ReelOffsetY(stop, c, ReelCount, TotalSpinFrames, StaggerFrames, CanvasHeight, Spins));
            Assert.Equal(0, SlotMachineGeometry.ReelOffsetY(stop + 5, c, ReelCount, TotalSpinFrames, StaggerFrames, CanvasHeight, Spins));
        }
    }

    [Fact]
    public void ReelOffsetY_ReturnsZeroForNonPositiveCanvas()
    {
        Assert.Equal(0, SlotMachineGeometry.ReelOffsetY(3, 0, ReelCount, TotalSpinFrames, StaggerFrames, 0, Spins));
    }
}
