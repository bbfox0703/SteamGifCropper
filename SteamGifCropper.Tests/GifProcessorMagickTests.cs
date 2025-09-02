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

    [Fact]
    public void SplitGif_RecalculatesAnimationTiming()
    {
        string tempDir = Directory.CreateTempSubdirectory().FullName;
        string input = GifTestHelper.CreateGradientGif(tempDir, 766, 100, 2, "red", "black");
        try
        {
            using var original = new MagickImageCollection(input);
            GifProcessor.SplitGif(input, tempDir);
            string partPath = Path.Combine(tempDir, $"{Path.GetFileNameWithoutExtension(input)}_Part1.gif");
            using var part = new MagickImageCollection(partPath);
            double partSum = 0;
            for (int i = 0; i < part.Count; i++)
            {
                partSum += (double)part[i].AnimationDelay / part[i].AnimationTicksPerSecond;
                Assert.Equal(100, (int)part[i].AnimationTicksPerSecond);
                double expected = (i + 1) / 15.0;
                Assert.True(Math.Abs(partSum - expected) < 1.0 / 100);
            }
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void SplitGif_HighFramerateUsesRoundedDelay()
    {
        string tempDir = Directory.CreateTempSubdirectory().FullName;
        string input = GifTestHelper.CreateGradientGif(tempDir, 766, 100, 4, "red", "black");
        try
        {
            GifProcessor.SplitGif(input, tempDir, 100);
            string partPath = Path.Combine(tempDir, $"{Path.GetFileNameWithoutExtension(input)}_Part1.gif");
            using var part = new MagickImageCollection(partPath);
            double partSum = 0;
            for (int i = 0; i < part.Count; i++)
            {
                partSum += (double)part[i].AnimationDelay / part[i].AnimationTicksPerSecond;
                Assert.Equal(100, (int)part[i].AnimationTicksPerSecond);
                Assert.Equal(1U, part[i].AnimationDelay);
                double expected = (i + 1) / 100.0;
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
