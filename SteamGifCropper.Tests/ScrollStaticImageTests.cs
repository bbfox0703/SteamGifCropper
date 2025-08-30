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
}
