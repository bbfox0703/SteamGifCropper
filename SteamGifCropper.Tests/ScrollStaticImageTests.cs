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

        GifProcessor.ScrollStaticImage(input, output, ScrollDirection.Right, 1, 1, true, 0, 10);

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

            GifProcessor.ScrollStaticImage(input, output, ScrollDirection.Right, 0, 1, true, 0, 10);
        
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

        GifProcessor.ScrollStaticImage(input, output, direction, 0, duration, true, 0, framerate);

        using var baseImg = new MagickImage(input);
        int distance = direction switch
        {
            ScrollDirection.Up or ScrollDirection.Down => (int)baseImg.Height,
            _ => (int)baseImg.Width
        };
        int expectedFrames = duration * framerate;

        using var collection = new MagickImageCollection(output);
        Assert.Equal(expectedFrames, collection.Count);

        int signX = direction switch
        {
            ScrollDirection.Right or ScrollDirection.RightUp or ScrollDirection.RightDown => 1,
            ScrollDirection.Left or ScrollDirection.LeftUp or ScrollDirection.LeftDown => -1,
            _ => 0
        };
        int signY = direction switch
        {
            ScrollDirection.Down or ScrollDirection.LeftDown or ScrollDirection.RightDown => 1,
            ScrollDirection.Up or ScrollDirection.LeftUp or ScrollDirection.RightUp => -1,
            _ => 0
        };

        int lastOffset = (int)Math.Round((double)distance * (expectedFrames - 1) / expectedFrames);
        int offsetX = 0, offsetY = 0;
        if (signX != 0)
        {
            offsetX = (signX * lastOffset) % (int)baseImg.Width;
            if (offsetX < 0) offsetX += (int)baseImg.Width;
        }
        if (signY != 0)
        {
            offsetY = (signY * lastOffset) % (int)baseImg.Height;
            if (offsetY < 0) offsetY += (int)baseImg.Height;
        }

        using var expectedLast = baseImg.Clone();
        expectedLast.Roll(offsetX, offsetY);
        double diff = collection[collection.Count - 1].Compare(expectedLast, ErrorMetric.RootMeanSquared);
        Assert.True(diff < 0.02);

        int step = (int)Math.Round((double)distance / expectedFrames);
        int totalScroll = lastOffset + step;
        Assert.InRange(totalScroll, distance - step, distance + step);

        Directory.Delete(tempDir, true);
    }

    [Fact]
    public void ScrollStaticImage_WithDuration_ProducesAnimatedGif()
    {
        string input = Path.Combine("TestData", "Scroll_test_1.png");
        string tempDir = Directory.CreateTempSubdirectory().FullName;
        string output = Path.Combine(tempDir, "out.gif");

        GifProcessor.ScrollStaticImage(input, output, ScrollDirection.Right, 0, 2, true, 0, 10);

        using var collection = new MagickImageCollection(output);
        Assert.True(collection.Count > 1);
        Assert.Equal((ushort)0, collection[0].AnimationIterations);
        double diff = collection[0].Compare(collection[1], ErrorMetric.RootMeanSquared);
        Assert.True(diff > 0.0);

        Directory.Delete(tempDir, true);
    }

    [Fact]
    public void ScrollStaticImage_MoveCountLimitsFrames()
    {
        string tempDir = Directory.CreateTempSubdirectory().FullName;
        string input = Path.Combine(tempDir, "input.png");
        using (var img = new MagickImage(MagickColors.Red, 10, 1))
        {
            img.Write(input);
        }
        string output = Path.Combine(tempDir, "out.gif");

        GifProcessor.ScrollStaticImage(input, output, ScrollDirection.Right, 4, 0, false, 5, 10);

        using var collection = new MagickImageCollection(output);
        // step 4 with width 10 caps moves to floor(10/4)=2
        Assert.Equal(2, collection.Count);

        Directory.Delete(tempDir, true);
    }
}
