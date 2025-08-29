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
        string input = Path.Combine("TestData", "wide.gif");
        bool created = EnsureGif(input, 766, 100);
        string tempDir = Directory.CreateTempSubdirectory().FullName;
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
            if (created)
            {
                //File.Delete(input);  // keep source file
            }
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
