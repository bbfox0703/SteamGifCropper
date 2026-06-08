using System;
using System.Linq;
using GifProcessorApp;

namespace SteamGifCropper.Tests;

public class GifEffectWindowTests
{
    // 10 frames at 0.1s each -> a 1.0s clip with starts 0.0, 0.1, ... 0.9.
    private static double[] UniformFrames() => Enumerable.Range(0, 10).Select(i => i * 0.1).ToArray();
    private const double GifSeconds = 1.0;

    [Fact]
    public void Clamp_NormalWindow_Unchanged()
    {
        var (start, dur) = GifEffectWindow.Clamp(0.30, 0.40, GifSeconds);
        Assert.Equal(0.30, start, 6);
        Assert.Equal(0.40, dur, 6);
    }

    [Fact]
    public void Clamp_StartPlusDurationOverruns_SlidesStartBack()
    {
        // 0.8 + 0.5 = 1.3 > 1.0 -> start slides to 1.0 - 0.5 = 0.5, duration preserved.
        var (start, dur) = GifEffectWindow.Clamp(0.80, 0.50, GifSeconds);
        Assert.Equal(0.50, start, 6);
        Assert.Equal(0.50, dur, 6);
    }

    [Theory]
    [InlineData(0.3, 1.5)] // duration exceeds clip
    [InlineData(0.3, 1.0)] // duration equals clip
    [InlineData(0.3, 0.0)] // non-positive duration
    public void Clamp_DurationCoversOrExceedsClip_WholeClip(double start, double dur)
    {
        var (s, d) = GifEffectWindow.Clamp(start, dur, GifSeconds);
        Assert.Equal(0.0, s, 6);
        Assert.Equal(GifSeconds, d, 6);
    }

    [Fact]
    public void Clamp_NegativeStart_ClampedToZero()
    {
        var (start, dur) = GifEffectWindow.Clamp(-0.2, 0.3, GifSeconds);
        Assert.Equal(0.0, start, 6);
        Assert.Equal(0.3, dur, 6);
    }

    [Fact]
    public void NearestFrameIndex_PicksClosest_TieGoesLower()
    {
        var frames = UniformFrames();
        Assert.Equal(3, GifEffectWindow.NearestFrameIndex(frames, 0.31));
        Assert.Equal(2, GifEffectWindow.NearestFrameIndex(frames, 0.25)); // tie 0.2/0.3 -> lower
        Assert.Equal(0, GifEffectWindow.NearestFrameIndex(frames, -1.0));
        Assert.Equal(9, GifEffectWindow.NearestFrameIndex(frames, 5.0));
    }

    [Fact]
    public void ResolveFrames_MidWindow_SnapsBothBoundaries()
    {
        var (startFrame, endFrame) = GifEffectWindow.ResolveFrames(UniformFrames(), GifSeconds, 0.25, 0.40);
        Assert.Equal(2, startFrame); // 0.25 -> frame 2 (tie low)
        Assert.Equal(6, endFrame);   // 0.65 -> frame 6
    }

    [Fact]
    public void ResolveFrames_DurationCoversClip_IsWholeClip()
    {
        var frames = UniformFrames();
        var (startFrame, endFrame) = GifEffectWindow.ResolveFrames(frames, GifSeconds, 0.0, 2.0);
        Assert.Equal(0, startFrame);
        Assert.Equal(frames.Length, endFrame); // end snaps to clip end
    }

    [Fact]
    public void ResolveFrames_TailWindow_EndSnapsToClipEnd()
    {
        // 0.85 + 0.30 overruns -> clamp to start 0.70, end 1.0 -> frames [7, 10).
        var (startFrame, endFrame) = GifEffectWindow.ResolveFrames(UniformFrames(), GifSeconds, 0.85, 0.30);
        Assert.Equal(7, startFrame);
        Assert.Equal(10, endFrame);
    }

    [Fact]
    public void ResolveFrames_AlwaysAtLeastOneFrame()
    {
        var (startFrame, endFrame) = GifEffectWindow.ResolveFrames(UniformFrames(), GifSeconds, 0.5, 0.001);
        Assert.True(endFrame > startFrame);
    }

    [Theory]
    [InlineData(1, 0.0)]  // before window
    [InlineData(2, 0.0)]  // at start -> aligned
    [InlineData(4, 0.5)]  // middle
    [InlineData(6, 1.0)]  // at end -> aligned
    [InlineData(8, 1.0)]  // after window
    public void FramePhase_RampsZeroToOneAcrossWindow(int frame, double expected)
    {
        Assert.Equal(expected, GifEffectWindow.FramePhase(frame, 2, 6), 6);
    }
}
