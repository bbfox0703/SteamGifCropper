using System;
using GifProcessorApp;

namespace SteamGifCropper.Tests;

public class QuicksandGeometryTests
{
    private const int WrapLength = 766;

    [Theory]
    [InlineData(-0.5, 0.0)]
    [InlineData(0.0, 0.0)]
    [InlineData(0.5, 0.5)]
    [InlineData(1.0, 1.0)]
    [InlineData(2.0, 1.0)]
    public void EaseInOutCubic_IsClampedAndSymmetric(double t, double expected)
    {
        Assert.Equal(expected, QuicksandGeometry.EaseInOutCubic(t), 6);
    }

    [Fact]
    public void EaseInOutCubic_IsMonotonicIncreasing()
    {
        double prev = -1.0;
        for (int i = 0; i <= 20; i++)
        {
            double v = QuicksandGeometry.EaseInOutCubic(i / 20.0);
            Assert.True(v >= prev, "ease-in-out cubic must be non-decreasing");
            Assert.InRange(v, 0.0, 1.0);
            prev = v;
        }
    }

    [Theory]
    [InlineData(300, 20)] // exact division
    [InlineData(300, 16)] // remainder distributed
    [InlineData(301, 16)]
    [InlineData(150, 7)]
    public void BandBounds_TileTheLengthExactlyWithoutGaps(int totalLength, int layers)
    {
        int expectedStart = 0;
        int coveredSize = 0;
        for (int b = 0; b < layers; b++)
        {
            var (start, size) = QuicksandGeometry.BandBounds(b, layers, totalLength);
            Assert.Equal(expectedStart, start); // contiguous: each band starts where the previous ended
            Assert.True(size >= 1, "every band must have at least 1px");
            expectedStart = start + size;
            coveredSize += size;
        }
        Assert.Equal(totalLength, coveredSize); // bands exactly cover the whole height
        Assert.Equal(totalLength, expectedStart);
    }

    [Fact]
    public void BandBounds_RemainderGoesToTheFirstBands()
    {
        // 301 / 16 = 18 r 13 -> first 13 bands are 19px, rest 18px.
        Assert.Equal(19, QuicksandGeometry.BandBounds(0, 16, 301).Size);
        Assert.Equal(19, QuicksandGeometry.BandBounds(12, 16, 301).Size);
        Assert.Equal(18, QuicksandGeometry.BandBounds(13, 16, 301).Size);
        Assert.Equal(18, QuicksandGeometry.BandBounds(15, 16, 301).Size);
    }

    [Fact]
    public void BandSpeed_BottomFast_PeaksAtBottomBand()
    {
        const int layers = 10;
        Assert.Equal(0.0, QuicksandGeometry.BandSpeed(0, layers, QuicksandFastBand.Bottom), 6);
        Assert.Equal(1.0, QuicksandGeometry.BandSpeed(layers - 1, layers, QuicksandFastBand.Bottom), 6);
    }

    [Fact]
    public void BandSpeed_TopFast_PeaksAtTopBand()
    {
        const int layers = 10;
        Assert.Equal(1.0, QuicksandGeometry.BandSpeed(0, layers, QuicksandFastBand.Top), 6);
        Assert.Equal(0.0, QuicksandGeometry.BandSpeed(layers - 1, layers, QuicksandFastBand.Top), 6);
    }

    [Fact]
    public void BandSpeed_MiddleFast_PeaksAtCentreAndDropsToEdges()
    {
        const int layers = 11; // odd -> exact centre at index 5
        Assert.Equal(0.0, QuicksandGeometry.BandSpeed(0, layers, QuicksandFastBand.Middle), 6);
        Assert.Equal(1.0, QuicksandGeometry.BandSpeed(5, layers, QuicksandFastBand.Middle), 6);
        Assert.Equal(0.0, QuicksandGeometry.BandSpeed(layers - 1, layers, QuicksandFastBand.Middle), 6);
    }

    [Fact]
    public void BandRevolutions_FastBandGetsMax_SlowBandGetsMin()
    {
        const int layers = 20;
        Assert.Equal(12, QuicksandGeometry.BandRevolutions(layers - 1, layers, 2, 12, QuicksandFastBand.Bottom, 1.0));
        Assert.Equal(2, QuicksandGeometry.BandRevolutions(0, layers, 2, 12, QuicksandFastBand.Bottom, 1.0));
    }

    [Fact]
    public void BandRevolutions_LinearViscosityInterpolatesMidband()
    {
        // layers=3, fraction of middle band = 0.5, speed=0.5, viscosity=1 -> 2 + (12-2)*0.5 = 7.
        Assert.Equal(7, QuicksandGeometry.BandRevolutions(1, 3, 2, 12, QuicksandFastBand.Bottom, 1.0));
    }

    [Fact]
    public void BandRevolutions_HigherViscosityKeepsSlowBandsSlower()
    {
        // viscosity 2 shapes speed^2: the middle band (speed 0.5) drops from 7 to 2+(10*0.25)=4.5 -> 5 (rounded).
        int linear = QuicksandGeometry.BandRevolutions(1, 3, 2, 12, QuicksandFastBand.Bottom, 1.0);
        int sticky = QuicksandGeometry.BandRevolutions(1, 3, 2, 12, QuicksandFastBand.Bottom, 2.0);
        Assert.True(sticky < linear, "higher viscosity should slow the interior bands");
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void BandOffset_StartsAndReturnsToOrigin(bool flowRight)
    {
        Assert.Equal(0, QuicksandGeometry.BandOffset(0.0, 12, WrapLength, flowRight));
        Assert.Equal(0, QuicksandGeometry.BandOffset(1.0, 12, WrapLength, flowRight)); // whole revolutions -> aligned
    }

    [Fact]
    public void BandOffset_StaysWithinWrapLength()
    {
        for (int i = 0; i <= 50; i++)
        {
            int off = QuicksandGeometry.BandOffset(i / 50.0, 7, WrapLength, true);
            Assert.InRange(off, 0, WrapLength - 1);
        }
    }

    [Fact]
    public void BandOffset_DirectionFlipMirrorsOffset()
    {
        // At a non-aligned mid-point, left flow is the wrap-complement of right flow.
        int right = QuicksandGeometry.BandOffset(0.37, 5, WrapLength, true);
        int left = QuicksandGeometry.BandOffset(0.37, 5, WrapLength, false);
        Assert.Equal((WrapLength - right) % WrapLength, left);
    }

    [Fact]
    public void BandOffset_ZeroRevolutionsNeverMoves()
    {
        for (int i = 0; i <= 10; i++)
        {
            Assert.Equal(0, QuicksandGeometry.BandOffset(i / 10.0, 0, WrapLength, true));
        }
    }
}
