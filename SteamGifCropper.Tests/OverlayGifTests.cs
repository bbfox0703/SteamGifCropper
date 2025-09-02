using System;
using System.IO;
using GifProcessorApp;
using ImageMagick;
using Xunit;

namespace SteamGifCropper.Tests;

public class OverlayGifTests
{
    private static string CreateBaseGif(string directory)
    {
        string path = Path.Combine(directory, "base.gif");
        using var collection = new MagickImageCollection();

        var frame1 = new MagickImage(MagickColors.Red, 2, 1)
        {
            AnimationDelay = 10,
            AnimationTicksPerSecond = 100
        };
        collection.Add(frame1);

        var frame2 = new MagickImage(MagickColors.Blue, 2, 1)
        {
            AnimationDelay = 10,
            AnimationTicksPerSecond = 100
        };
        collection.Add(frame2);

        collection.Write(path);
        return path;
    }

    private static string CreateOverlayGif(string directory)
    {
        string path = Path.Combine(directory, "overlay.gif");
        using var collection = new MagickImageCollection();
        for (int i = 0; i < 4; i++)
        {
            var frame = new MagickImage(MagickColors.Black, 1, 1)
            {
                AnimationDelay = 5,
                AnimationTicksPerSecond = 100
            };
            collection.Add(frame);
        }

        collection.Write(path);
        return path;
    }

    private static string CreateOverlayGifWithColors(string directory, MagickColor[] colors)
    {
        string path = Path.Combine(directory, "overlay_colors.gif");
        using var collection = new MagickImageCollection();
        foreach (var color in colors)
        {
            var frame = new MagickImage(color, 1, 1)
            {
                AnimationDelay = 5,
                AnimationTicksPerSecond = 100
            };
            collection.Add(frame);
        }

        collection.Write(path);
        return path;
    }

    [Fact]
    public void OverlayGif_UsesOverlayTimingAndAlignsBaseFrames()
    {
        string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        try
        {
            string basePath = CreateBaseGif(tempDir);
            string overlayPath = CreateOverlayGif(tempDir);
            string outputPath = Path.Combine(tempDir, "output.gif");

            GifProcessor.OverlayGif(basePath, overlayPath, outputPath);

            using var result = new MagickImageCollection(outputPath);
            result.Coalesce();
            Assert.Equal(4, result.Count);

            for (int i = 0; i < result.Count; i++)
            {
                Xunit.Assert.Equal(5, (int)result[i].AnimationDelay);

                var pixel = result[i].GetPixels().GetPixel(1, 0).ToColor();
                var expected = i < 2 ? MagickColors.Red : MagickColors.Blue;
                Xunit.Assert.Equal(expected, pixel);
            }
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void OverlayGif_BaseFramesAsIs_UsesBaseTiming()
    {
        string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        try
        {
            string basePath = CreateBaseGif(tempDir);
            var colors = new[] { MagickColors.Black, MagickColors.White, MagickColors.Green, MagickColors.Yellow };
            string overlayPath = CreateOverlayGifWithColors(tempDir, colors);
            string outputPath = Path.Combine(tempDir, "output.gif");

            GifProcessor.OverlayGif(basePath, overlayPath, outputPath, resampleBase: false);

            using var baseGif = new MagickImageCollection(basePath);
            baseGif.Coalesce();
            using var result = new MagickImageCollection(outputPath);
            result.Coalesce();

            Assert.Equal(baseGif.Count, result.Count);
            for (int i = 0; i < baseGif.Count; i++)
            {
                Assert.Equal(baseGif[i].AnimationDelay, result[i].AnimationDelay);
            }

            var expectedOverlayColors = new[] { MagickColors.Black, MagickColors.Green };
            for (int i = 0; i < result.Count; i++)
            {
                var overlayPixel = result[i].GetPixels().GetPixel(0, 0).ToColor();
                Assert.Equal(expectedOverlayColors[i], overlayPixel);

                var basePixel = result[i].GetPixels().GetPixel(1, 0).ToColor();
                var expectedBase = i == 0 ? MagickColors.Red : MagickColors.Blue;
                Assert.Equal(expectedBase, basePixel);
            }
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }
}

