using ImageMagick;
using GifProcessorApp;

namespace SteamGifCropper.Tests;

// Runtime smoke tests for the Magick pixel read/write path (RippleField itself is covered by
// RippleFieldTests). Verifies RenderFrame produces a valid same-size image without throwing.
public class RippleRendererTests
{
    private static RippleMedium Medium() => new RippleMedium
    {
        WaveSpeed = 100.0,
        Wavelength = 20.0,
        SpatialDamping = 0.01,
        TimeDamping = 0.5,
        Strength = 5.0,
        Threshold = 0.02,
    };

    [Fact]
    public void RenderFrame_PreservesDimensions()
    {
        using var src = new MagickImage(MagickColors.SteelBlue, 40, 30);
        var drops = new[] { new RippleDrop(20, 15, 0.0, 1.0) };
        using var outImg = RippleRenderer.RenderFrame(src, 0.2, drops, Medium());
        Assert.Equal(40u, outImg.Width);
        Assert.Equal(30u, outImg.Height);
    }

    [Fact]
    public void RenderFrame_NoActiveDrops_ReturnsSameSizeImage()
    {
        using var src = new MagickImage(MagickColors.SteelBlue, 16, 16);
        using var outImg = RippleRenderer.RenderFrame(src, 0.0, new RippleDrop[0], Medium());
        Assert.Equal(16u, outImg.Width);
        Assert.Equal(16u, outImg.Height);
    }

    [Fact]
    public void RenderFrame_DropOutsideImage_DoesNotThrow()
    {
        using var src = new MagickImage(MagickColors.SteelBlue, 24, 18);
        var drops = new[] { new RippleDrop(100, 80, 0.0, 1.0) }; // well outside the 24x18 image
        using var outImg = RippleRenderer.RenderFrame(src, 0.3, drops, Medium());
        Assert.Equal(24u, outImg.Width);
        Assert.Equal(18u, outImg.Height);
    }

    [Fact]
    public void MixedRenderedAndClonedFrames_OptimizeDoesNotThrow()
    {
        // Reproduces the play-along build: a rendered (ReadPixels) frame next to a cloned source frame.
        // Without uniform page metadata, Optimize() raises "image pages are not coalesced". RenderFrame
        // ResetPage()s its output; the engine ResetPage()s the clones — together they must Optimize cleanly.
        using var src = new MagickImage(MagickColors.SteelBlue, 48, 32);
        var drops = new[] { new RippleDrop(24, 16, 0.0, 1.0) };
        using var coll = new MagickImageCollection();

        var rendered = RippleRenderer.RenderFrame(src, 0.15, drops, Medium());
        rendered.AnimationDelay = 5;
        coll.Add(rendered);

        var cloned = (MagickImage)src.Clone();
        cloned.ResetPage();
        cloned.AnimationDelay = 5;
        coll.Add(cloned);

        coll.Optimize(); // must not throw
        Assert.Equal(2, coll.Count);
    }
}
