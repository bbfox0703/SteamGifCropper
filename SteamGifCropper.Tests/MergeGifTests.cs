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

    private static readonly string[] SampleGifFiles =
    {
        Path.Combine("TestData", "test1_400x400_10s.gif"),
        Path.Combine("TestData", "test2_100x250_15s.gif"),
        Path.Combine("TestData", "test3_150x1920_8s.gif"),
        Path.Combine("TestData", "test4_1920x1080_10s.gif"),
        Path.Combine("TestData", "test5_886x1920_12s.gif")
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

    [Theory]
    [MemberData(nameof(GifCounts))]
    public async Task MergeMultipleGifs_UsingSampleGifs_MergesCorrectly(int gifCount)
    {
        string tempDir = Directory.CreateTempSubdirectory().FullName;
        ulong originalMemory = ResourceLimits.Memory;
        ulong originalDisk = ResourceLimits.Disk;
        try
        {
            ResourceLimits.Memory = 2UL * 1024UL * 1024UL * 1024UL;
            ResourceLimits.Disk = 4UL * 1024UL * 1024UL * 1024UL;

            var gifPaths = SampleGifFiles.Take(gifCount).ToList();

            int expectedWidth = 0;
            int expectedHeight = 0;
            double shortestDuration = double.MaxValue;
            foreach (var path in gifPaths)
            {
                using var col = new MagickImageCollection(path);
                col.Coalesce();
                expectedWidth += (int)col[0].Width;
                expectedHeight = Math.Max(expectedHeight, (int)col[0].Height);
                double duration = col.Sum(f => (double)f.AnimationDelay / f.AnimationTicksPerSecond);
                shortestDuration = Math.Min(shortestDuration, duration);
            }

            var form = new GifToolMainForm();
            int targetFramerate = 10;
            int expectedFrames = Math.Max(1, (int)(shortestDuration * targetFramerate));

            string fastOutput = Path.Combine(tempDir, "merged_fast.gif");
            await GifProcessor.MergeMultipleGifs(gifPaths, fastOutput, form, targetFramerate, true);
            using var fast = new MagickImageCollection(fastOutput);
            Assert.Equal(expectedWidth, (int)fast[0].Width);
            Assert.Equal(expectedHeight, (int)fast[0].Height);
            Assert.Equal(expectedFrames, fast.Count);
            int fastColors = (int)fast[0].TotalColors;
            Assert.True(fastColors <= 256);

            string qualityOutput = Path.Combine(tempDir, "merged_quality.gif");
            await GifProcessor.MergeMultipleGifs(gifPaths, qualityOutput, form, targetFramerate, false);
            using var quality = new MagickImageCollection(qualityOutput);
            Assert.Equal(expectedWidth, (int)quality[0].Width);
            Assert.Equal(expectedHeight, (int)quality[0].Height);
            Assert.Equal(expectedFrames, quality.Count);
            int qualityColors = (int)quality[0].TotalColors;
            Assert.True(qualityColors <= 256);
        }
        finally
        {
            ResourceLimits.Memory = originalMemory;
            ResourceLimits.Disk = originalDisk;
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
            string mergedPath = Path.Combine(tempDir, "merged.gif");
            method.Invoke(null, new object?[] { collections, mergedPath, form, false });
            using var merged = new MagickImageCollection(mergedPath);
            Assert.Equal(766U, merged[0].Width);

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

    [Fact]
    public void MergeGifsHorizontally_UsingSampleGifs_MergesCorrectly()
    {
        string tempDir = Directory.CreateTempSubdirectory().FullName;
        var collections = new MagickImageCollection[5];
        ulong originalMemory = ResourceLimits.Memory;
        ulong originalDisk = ResourceLimits.Disk;
        try
        {
            ResourceLimits.Memory = 2UL * 1024UL * 1024UL * 1024UL;
            ResourceLimits.Disk = 4UL * 1024UL * 1024UL * 1024UL;

            for (int i = 0; i < 5; i++)
            {
                var col = new MagickImageCollection(SampleGifFiles[i]);
                col.Coalesce();
                collections[i] = col;
            }

            int expectedHeight = collections.Max(c => (int)c[0].Height);
            int expectedFrames = collections.Max(c => c.Count);

            var form = new GifToolMainForm();
            var method = typeof(GifProcessor).GetMethod("MergeGifsHorizontally", BindingFlags.NonPublic | BindingFlags.Static)!;
            string outputPath = Path.Combine(tempDir, "merged.gif");
            method.Invoke(null, new object?[] { collections, outputPath, form, false });

            using var merged = new MagickImageCollection(outputPath);
            Assert.Equal(766, (int)merged[0].Width);
            Assert.Equal(expectedHeight, (int)merged[0].Height);
            Assert.Equal(expectedFrames, merged.Count);
            Assert.True(merged[0].TotalColors <= 256);
        }
        finally
        {
            foreach (var c in collections)
            {
                c?.Dispose();
            }

            ResourceLimits.Memory = originalMemory;
            ResourceLimits.Disk = originalDisk;
            Directory.Delete(tempDir, true);
        }
    }
}
