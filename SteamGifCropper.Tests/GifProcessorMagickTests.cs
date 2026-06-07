using System;
using System.IO;
using ImageMagick;
using GifProcessorApp;

namespace SteamGifCropper.Tests;

public class GifProcessorMagickTests
{
    [Fact]
    public void ResizeGifTo766_ResizesWidth()
    {
        string input = Path.Combine("TestData", "small.gif");
        bool created = EnsureGif(input, 100, 100);
        string tempDir = Directory.CreateTempSubdirectory().FullName;
        string output = Path.Combine(tempDir, "resized.gif");
        try
        {
            GifProcessor.ResizeGifTo766(input, output);
            using var image = new MagickImage(output);
            Assert.Equal(766U, image.Width);
        }
        finally
        {
            Directory.Delete(tempDir, true);
            if (created)
            {
                //File.Delete(input);  // keep source file
            }
        }
    }

    [Fact]
    public void SplitGif_CreatesFivePartsWithCorrectWidth()
    {
        string tempDir = Directory.CreateTempSubdirectory().FullName;
        string input = GifTestHelper.CreateGradientGif(tempDir, 766, 100, 1, "red", "black");
        try
        {
            GifProcessor.SplitGif(input, tempDir);
            var files = Directory.GetFiles(tempDir, "*_Part*.gif");
            Assert.Equal(5, files.Length);
            foreach (var file in files)
            {
                using var image = new MagickImage(file);
                Assert.Equal(150U, image.Width);
            }
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    // SplitGif preserves the source frame delays/ticks unchanged. The earlier
    // "recalculate to a target framerate" logic this test was originally written
    // against was removed in commit dab2d14 ("fix FPS") and the targetFramerate
    // parameter dropped in 3aba7e7 ("fix error"); RecalculateGifDelays now returns
    // the original delays verbatim. CreateGradientGif bakes delay=10 at 100
    // ticks/sec, so every part frame stays 10/100 = 0.1s and cumulative = (i+1)/10.
    [Fact]
    public void SplitGif_PreservesAnimationTiming()
    {
        string tempDir = Directory.CreateTempSubdirectory().FullName;
        string input = GifTestHelper.CreateGradientGif(tempDir, 766, 100, 2, "red", "black");
        try
        {
            GifProcessor.SplitGif(input, tempDir);
            string partPath = Path.Combine(tempDir, $"{Path.GetFileNameWithoutExtension(input)}_Part1.gif");
            using var part = new MagickImageCollection(partPath);
            double partSum = 0;
            for (int i = 0; i < part.Count; i++)
            {
                partSum += (double)part[i].AnimationDelay / part[i].AnimationTicksPerSecond;
                Assert.Equal(100, (int)part[i].AnimationTicksPerSecond);
                Assert.Equal(10U, part[i].AnimationDelay);
                double expected = (i + 1) / 10.0;
                Assert.True(Math.Abs(partSum - expected) < 1.0 / 100);
            }
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    // Same preserved-timing contract as above, exercised over more frames. This was
    // originally SplitGif_HighFramerateUsesRoundedDelay, which called the now-removed
    // SplitGif(input, tempDir, 100) overload and expected delay==1 (recalculated to
    // 100 fps). With recalculation gone, a 4-frame source at delay=10/100 ticks keeps
    // delay=10 per frame, so cumulative = (i+1)/10.
    [Fact]
    public void SplitGif_PreservesDelayAcrossFrames()
    {
        string tempDir = Directory.CreateTempSubdirectory().FullName;
        string input = GifTestHelper.CreateGradientGif(tempDir, 766, 100, 4, "red", "black");
        try
        {
            GifProcessor.SplitGif(input, tempDir);
            string partPath = Path.Combine(tempDir, $"{Path.GetFileNameWithoutExtension(input)}_Part1.gif");
            using var part = new MagickImageCollection(partPath);
            double partSum = 0;
            for (int i = 0; i < part.Count; i++)
            {
                partSum += (double)part[i].AnimationDelay / part[i].AnimationTicksPerSecond;
                Assert.Equal(100, (int)part[i].AnimationTicksPerSecond);
                Assert.Equal(10U, part[i].AnimationDelay);
                double expected = (i + 1) / 10.0;
                Assert.True(Math.Abs(partSum - expected) < 1.0 / 100);
            }
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    private static bool EnsureGif(string path, int width, int height)
    {
        if (File.Exists(path))
        {
            return false;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        using var image = new MagickImage(MagickColors.Red, (uint)width, (uint)height);
        image.Write(path);
        return true;
    }
}
