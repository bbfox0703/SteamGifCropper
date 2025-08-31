using System.IO;
using GifProcessorApp;
using ImageMagick;

namespace SteamGifCropper.Tests;

public class ScrollStaticImageTests
{
    [Fact]
    public void ScrollStaticImage_CreatesExpectedFrames()
    {
        string tempDir = Directory.CreateTempSubdirectory().FullName;
        string input = Path.Combine(tempDir, "input.png");
        using (var img = new MagickImage(MagickColors.Red, 10, 1))
        {
            img.Write(input);
        }
        string output = Path.Combine(tempDir, "out.gif");

        GifProcessor.ScrollStaticImage(input, output, ScrollDirection.Right, 1, 1, true, 10);

        using var collection = new MagickImageCollection(output);
        Assert.Equal(10, collection.Count);

        Directory.Delete(tempDir, true);
    }

    [Theory]
    [InlineData("Scroll_test_1.png")]
    [InlineData("Scroll_test_2.png")]
    public void ScrollStaticImage_LargeImagesRespectResourceLimits(string fileName)
    {
        string input = Path.Combine("TestData", fileName);
        string tempDir = Directory.CreateTempSubdirectory().FullName;
        string output = Path.Combine(tempDir, "out.gif");

        ulong originalMemory = ResourceLimits.Memory;
        ulong originalDisk = ResourceLimits.Disk;
        try
        {
            ResourceLimits.Memory = 8UL * 1024UL * 1024UL;   // 8 MB to force disk fallback
            ResourceLimits.Disk = 128UL * 1024UL * 1024UL;  // 128 MB temporary disk space

            GifProcessor.ScrollStaticImage(input, output, ScrollDirection.Right, 10, 1, false, 10);

            using var collection = new MagickImageCollection(output);
            Assert.True(collection.Count > 1);
        }
        finally
        {
            ResourceLimits.Memory = originalMemory;
            ResourceLimits.Disk = originalDisk;
            Directory.Delete(tempDir, true);
        }
    }
}
