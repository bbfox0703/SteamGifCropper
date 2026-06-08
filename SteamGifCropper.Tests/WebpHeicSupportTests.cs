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
}
