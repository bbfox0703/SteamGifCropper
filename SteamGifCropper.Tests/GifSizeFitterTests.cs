using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GifProcessorApp;

public class GifSizeFitterTests
{
    // Deterministic, monotonic size model: smaller for fewer colors and higher lossy.
    // bytes = colors*20 - lossy*2  (kept tiny so the fake allocations stay cheap).
    private static Func<byte[], GifsicleWrapper.GifsicleOptions, Task<byte[]>> Model(List<(int colors, int lossy)>? calls = null)
    {
        return (src, opts) =>
        {
            calls?.Add((opts.Colors, opts.Lossy));
            int size = Math.Max(1, opts.Colors * 20 - opts.Lossy * 2);
            return Task.FromResult(new byte[size]);
        };
    }

    private static GifSizeFitter.FitOptions Opts(long target, int maxAttempts = 14) => new GifSizeFitter.FitOptions
    {
        TargetBytes = target,
        MaxAttempts = maxAttempts,
        MaxLossy = 200,
        LossyStep = 10,
        ColorLadder = new[] { 256, 224, 192, 160, 128, 96, 64 }
    };

    [Fact]
    public async Task FitAsync_MaximizesColorsThenMinimizesLossy()
    {
        // 256 colors fits at max lossy (4720 <= 5000), so it keeps 256 colors and binary-searches
        // the lowest lossy: 5120 - 2*lossy <= 5000  =>  lossy >= 60.
        var calls = new List<(int, int)>();
        var result = await GifSizeFitter.FitAsync(new byte[10], Opts(5000), Model(calls));

        Assert.True(result.TargetMet);
        Assert.Equal(256, result.Colors);
        Assert.Equal(60, result.Lossy);
        Assert.Equal(5000, result.Bytes);
        Assert.Equal(calls.Count, result.Attempts);
    }

    [Fact]
    public async Task FitAsync_DropsColorsWhenHighColorsCannotFit()
    {
        // Target 4000: 256@200=4720 (no), 224@200=4080 (no), 192@200=3440 (yes) -> feasible 192,
        // then lowest lossy at 192: 3840 - 2*lossy <= 4000 is true even at lossy 0 -> lossy 0.
        var result = await GifSizeFitter.FitAsync(new byte[10], Opts(4000), Model());

        Assert.True(result.TargetMet);
        Assert.Equal(192, result.Colors);
        Assert.Equal(0, result.Lossy);
        Assert.True(result.Bytes <= 4000);
    }

    [Fact]
    public async Task FitAsync_TargetUnreachable_ReturnsSmallestAndReportsFailure()
    {
        // Even 64@200 = 880 > 500, so nothing fits.
        var result = await GifSizeFitter.FitAsync(new byte[10], Opts(500), Model());

        Assert.False(result.TargetMet);
        Assert.Equal(64, result.Colors); // smallest achievable in the ladder
        Assert.Equal(880, result.Bytes);
    }

    [Fact]
    public async Task FitAsync_RespectsMaxAttempts()
    {
        // One attempt only: phase 1 finds 256@200 fits, phase 2 never runs (budget exhausted).
        var calls = new List<(int, int)>();
        var result = await GifSizeFitter.FitAsync(new byte[10], Opts(5000, maxAttempts: 1), Model(calls));

        Assert.Single(calls);
        Assert.Equal(1, result.Attempts);
        Assert.Equal(200, result.Lossy);
        Assert.True(result.TargetMet);
    }

    [Fact]
    public async Task FitAsync_EmptySource_Throws()
    {
        await Assert.ThrowsAsync<ArgumentException>(
            () => GifSizeFitter.FitAsync(Array.Empty<byte>(), Opts(5000), Model()));
    }
}
