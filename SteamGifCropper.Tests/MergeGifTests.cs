using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using GifProcessorApp;
using ImageMagick;
using Xunit;

namespace SteamGifCropper.Tests;

public class MergeGifTests
{
    public static IEnumerable<object[]> GifCounts => new[]
    {
        new object[] { 2 },
        new object[] { 3 },
        new object[] { 4 },
        new object[] { 5 }
    };

    [Theory]
    [MemberData(nameof(GifCounts))]
    public async Task MergeMultipleGifs_MergesCorrectly(int gifCount)
    {
        string tempDir = Directory.CreateTempSubdirectory().FullName;
        try
        {
            int[] widths = { 20, 30, 40, 50, 60 };
            int[] frames = { 4, 3, 2, 5, 6 };
            string[] colors = { "red", "green", "blue", "yellow", "purple" };
            var gifPaths = new List<string>();
            for (int i = 0; i < gifCount; i++)
            {
                gifPaths.Add(GifTestHelper.CreateGradientGif(tempDir, widths[i], 20, frames[i], colors[i], "black"));
            }

            var form = new GifToolMainForm();
            int targetFramerate = 10;

            string fastOutput = Path.Combine(tempDir, "merged_fast.gif");
            await GifProcessor.MergeMultipleGifs(gifPaths, fastOutput, form, targetFramerate, true);
            using var fast = new MagickImageCollection(fastOutput);
            Assert.Equal(widths.Take(gifCount).Sum(), (int)fast[0].Width);
            Assert.Equal(frames.Take(gifCount).Min(), fast.Count);
            int fastColors = (int)fast[0].TotalColors;
            Assert.True(fastColors <= 256);

            string qualityOutput = Path.Combine(tempDir, "merged_quality.gif");
            await GifProcessor.MergeMultipleGifs(gifPaths, qualityOutput, form, targetFramerate, false);
            using var quality = new MagickImageCollection(qualityOutput);
            Assert.Equal(widths.Take(gifCount).Sum(), (int)quality[0].Width);
            Assert.Equal(frames.Take(gifCount).Min(), quality.Count);
            int qualityColors = (int)quality[0].TotalColors;
            Assert.True(qualityColors <= 256);

            Assert.NotEqual(fastColors, qualityColors);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void MergeGifsHorizontally_SplitsCorrectlyAndReusesPalette()
    {
        string tempDir = Directory.CreateTempSubdirectory().FullName;
        try
        {
            int[] widths = { 153, 153, 154, 153, 153 };
            string[] colors = { "red", "green", "blue", "yellow", "purple" };
            int height = 50;
            var paths = new List<string>();
            for (int i = 0; i < 5; i++)
            {
                paths.Add(GifTestHelper.CreateGradientGif(tempDir, widths[i], height, 2, colors[i], "black"));
            }

            var collections = paths.Select(p => new MagickImageCollection(p)).ToArray();
            foreach (var c in collections) c.Coalesce();

            GifProcessor.PaletteQuantizeCallCount = 0;
            var form = new GifToolMainForm();
            var method = typeof(GifProcessor).GetMethod("MergeGifsHorizontally", BindingFlags.NonPublic | BindingFlags.Static)!;
            using var merged = (MagickImageCollection)method.Invoke(null, new object?[] { collections, form, false })!;
            Assert.Equal(766U, merged[0].Width);

            string mergedPath = Path.Combine(tempDir, "merged.gif");
            merged.Write(mergedPath);
            GifProcessor.SplitGif(mergedPath, tempDir);
            var files = Directory.GetFiles(tempDir, "*_Part*.gif");
            Assert.Equal(5, files.Length);
            foreach (var file in files)
            {
                using var img = new MagickImage(file);
                Assert.Equal(150U, img.Width);
                Assert.Equal((uint)(height + 100), img.Height);
            }

            Assert.Equal(1, GifProcessor.PaletteQuantizeCallCount);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }
}
