using ImageMagick;
using GifProcessorApp;

// Exercises the dynamic (overlap) transition engine: the frame count math the assembler relies on,
// and that GenerateTransition emits the right number of correctly-sized frames for representative
// effects (including Ripple, which runs the RippleRenderer resample).
public class TransitionGeneratorTests
{
    [Theory]
    [InlineData(0.5f, 10, 20, 20, 5)]   // round(5) = 5, clamp min(20,20)
    [InlineData(0f, 10, 20, 20, 0)]     // no duration -> no transition
    [InlineData(2f, 10, 3, 8, 3)]       // round(20) clamped to min(3,8) = 3
    [InlineData(0.5f, 0, 20, 20, 0)]    // no fps -> no transition
    public void GetFrameCount_Clamps(float dur, int fps, int fromCount, int toCount, int expected)
    {
        Assert.Equal(expected, TransitionGenerator.GetFrameCount(dur, fps, fromCount, toCount));
    }

    private static MagickImageCollection SolidClip(int frames, MagickColor color, int w, int h)
    {
        var c = new MagickImageCollection();
        for (int i = 0; i < frames; i++)
            c.Add(new MagickImage(color, (uint)w, (uint)h));
        return c;
    }

    [Theory]
    [InlineData(TransitionType.Fade)]
    [InlineData(TransitionType.CrossFade)]
    [InlineData(TransitionType.SlideLeft)]
    [InlineData(TransitionType.ZoomIn)]
    [InlineData(TransitionType.Dissolve)]
    [InlineData(TransitionType.IrisOpen)]
    [InlineData(TransitionType.IrisClose)]
    [InlineData(TransitionType.WipeLeft)]
    [InlineData(TransitionType.WipeDiagonal)]
    [InlineData(TransitionType.DipToBlack)]
    [InlineData(TransitionType.BlurDissolve)]
    [InlineData(TransitionType.Ripple)]
    public void GenerateTransition_ProducesExpectedFramesAndSize(TransitionType type)
    {
        using var from = SolidClip(6, MagickColors.Red, 32, 24);
        using var to = SolidClip(6, MagickColors.Blue, 32, 24);
        const int fps = 10;
        const float dur = 0.4f; // round(4) = 4
        int expected = TransitionGenerator.GetFrameCount(dur, fps, 6, 6);

        using var frames = TransitionGenerator.GenerateTransition(from, to, type, dur, fps);

        Assert.Equal(expected, frames.Count);
        foreach (var f in frames)
        {
            Assert.Equal(32u, f.Width);
            Assert.Equal(24u, f.Height);
        }
    }

    [Fact]
    public void GenerateTransition_None_ReturnsEmpty()
    {
        using var from = SolidClip(3, MagickColors.Red, 16, 16);
        using var to = SolidClip(3, MagickColors.Blue, 16, 16);
        using var frames = TransitionGenerator.GenerateTransition(from, to, TransitionType.None, 0.5f, 10);
        Assert.Empty(frames);
    }
}
