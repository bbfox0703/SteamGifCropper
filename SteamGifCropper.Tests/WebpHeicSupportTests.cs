using System;
using System.IO;
using ImageMagick;
using SteamGifCropper;

// Verifies the bundled Magick.NET-Q8 actually decodes HEIC and round-trips WebP, and that the
// input validator accepts both formats. These guard the "global image support" feature.
public class WebpHeicSupportTests
{
    private static readonly string HeicSample = Path.Combine("TestData", "Sample.HEIC");

    [Fact]
    public void Validator_AcceptsRealHeic()
    {
        if (!File.Exists(HeicSample))
            return; // asset optional in lightweight checkouts
        ImageInputValidator.ValidateImage(HeicSample); // must not throw
    }

    [Fact]
    public void Magick_DecodesRealHeic()
    {
        if (!File.Exists(HeicSample))
            return;
        using var image = new MagickImage(HeicSample);
        Assert.True(image.Width > 0);
        Assert.True(image.Height > 0);
    }

    [Fact]
    public void Magick_RoundTripsWebp()
    {
        using var src = new MagickImage(MagickColors.Red, 8, 8);
        byte[] webp = src.ToByteArray(MagickFormat.WebP);
        Assert.NotEmpty(webp);
        using var read = new MagickImage(webp);
        Assert.Equal(8u, read.Width);
        Assert.Equal(8u, read.Height);
    }

    [Fact]
    public void Validator_AcceptsWebpMagicBytes()
    {
        // "RIFF" + size + "WEBP"
        byte[] header = { 0x52, 0x49, 0x46, 0x46, 0x10, 0x00, 0x00, 0x00, 0x57, 0x45, 0x42, 0x50, 0x00, 0x00, 0x00, 0x00 };
        string tmp = Path.Combine(Path.GetTempPath(), "wh_" + Guid.NewGuid().ToString("N")[..8] + ".webp");
        File.WriteAllBytes(tmp, header);
        try { ImageInputValidator.ValidateImage(tmp); }
        finally { File.Delete(tmp); }
    }

    [Fact]
    public void ValidateGifOrWebp_AcceptsGifAndWebp()
    {
        byte[] gif = { 0x47, 0x49, 0x46, 0x38, 0x39, 0x61, 0x01, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00, 0x3B };
        byte[] webp = { 0x52, 0x49, 0x46, 0x46, 0x10, 0x00, 0x00, 0x00, 0x57, 0x45, 0x42, 0x50, 0x00, 0x00, 0x00, 0x00 };
        string g = Path.Combine(Path.GetTempPath(), "gw_" + Guid.NewGuid().ToString("N")[..8] + ".gif");
        string w = Path.Combine(Path.GetTempPath(), "gw_" + Guid.NewGuid().ToString("N")[..8] + ".webp");
        File.WriteAllBytes(g, gif);
        File.WriteAllBytes(w, webp);
        try
        {
            ImageInputValidator.ValidateGifOrWebp(g);
            ImageInputValidator.ValidateGifOrWebp(w);
        }
        finally { File.Delete(g); File.Delete(w); }
    }

    [Fact]
    public void ValidateGifOrWebp_RejectsPng()
    {
        byte[] png = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x00 };
        string p = Path.Combine(Path.GetTempPath(), "gw_" + Guid.NewGuid().ToString("N")[..8] + ".png");
        File.WriteAllBytes(p, png);
        try { Assert.Throws<InvalidOperationException>(() => ImageInputValidator.ValidateGifOrWebp(p)); }
        finally { File.Delete(p); }
    }

    [Fact]
    public void Magick_RoundTripsAnimatedWebp()
    {
        // Build a 2-frame animation and write it as animated WebP, then read it back as a collection.
        using var src = new MagickImageCollection();
        src.Add(new MagickImage(MagickColors.Red, 8, 8));
        src.Add(new MagickImage(MagickColors.Blue, 8, 8));
        foreach (var f in src) { f.AnimationDelay = 10; }
        byte[] webp = src.ToByteArray(MagickFormat.WebP);
        Assert.NotEmpty(webp);
        using var read = new MagickImageCollection(webp);
        Assert.Equal(2, read.Count);
    }
}
