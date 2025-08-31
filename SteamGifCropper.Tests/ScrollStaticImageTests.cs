using System;
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

            GifProcessor.ScrollStaticImage(input, output, ScrollDirection.Right, 0, 1, true, 10);
        
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

    [Theory]
    [InlineData("Scroll_test_1.png", ScrollDirection.Right, 2, 10)]
    [InlineData("Scroll_test_2.png", ScrollDirection.Down, 3, 10)]
    public void ScrollStaticImage_DurationProducesExpectedScroll(string fileName, ScrollDirection direction, int duration, int framerate)
    {
        string input = Path.Combine("TestData", fileName);
        string tempDir = Directory.CreateTempSubdirectory().FullName;
        string output = Path.Combine(tempDir, "out.gif");

        GifProcessor.ScrollStaticImage(input, output, direction, 0, duration, true, framerate);

        using var baseImg = new MagickImage(input);
        int distance = direction switch
        {
            ScrollDirection.Up or ScrollDirection.Down => (int)baseImg.Height,
            _ => (int)baseImg.Width
        };
        int expectedFrames = duration * framerate;
        int expectedStep = Math.Max(1, (int)Math.Round((double)distance / expectedFrames));

        using var collection = new MagickImageCollection(output);
        Assert.Equal(expectedFrames, collection.Count);

        int offsetX = 0, offsetY = 0;
        if (direction is ScrollDirection.Right or ScrollDirection.Left or ScrollDirection.LeftUp or ScrollDirection.LeftDown or ScrollDirection.RightUp or ScrollDirection.RightDown)
            offsetX = expectedStep * (expectedFrames - 1) % (int)baseImg.Width;
        if (direction is ScrollDirection.Up or ScrollDirection.Down or ScrollDirection.LeftUp or ScrollDirection.RightUp or ScrollDirection.LeftDown or ScrollDirection.RightDown)
            offsetY = expectedStep * (expectedFrames - 1) % (int)baseImg.Height;

        using var expectedLast = baseImg.Clone();
        expectedLast.Roll(offsetX, offsetY);
        double diff = collection[collection.Count - 1].Compare(expectedLast, ErrorMetric.RootMeanSquared);
        Assert.True(diff < 0.02);

        int totalScroll = expectedStep * expectedFrames;
        Assert.InRange(totalScroll, distance - expectedStep, distance + expectedStep);

        Directory.Delete(tempDir, true);
    }
}
