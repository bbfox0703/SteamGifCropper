using System;
using System.Reflection;
using System.Collections.Generic;
using System.IO;
using GifProcessorApp;
using ImageMagick;

namespace SteamGifCropper.Tests;

public class GifProcessorTests
{
    private static MethodInfo GetMethod(string name) =>
        typeof(GifProcessor).GetMethod(name, BindingFlags.NonPublic | BindingFlags.Static) ??
        throw new InvalidOperationException($"Method '{name}' not found.");

    private static readonly byte[] SampleGif =
    {
        0x47, 0x49, 0x46, 0x38, 0x39, 0x61, 0x00, 0x00, 0x00, 0x00, 0x3B
    };

    public static IEnumerable<object[]> CanvasWidthData => new[]
    {
        new object[] { (uint)766, true },
        new object[] { (uint)774, true },
        new object[] { (uint)760, false },
        new object[] { (uint)0, false }
    };

    [Theory]
    [MemberData(nameof(CanvasWidthData))]
    public void IsValidCanvasWidth_ReturnsExpected(uint width, bool expected)
    {
        var method = GetMethod("IsValidCanvasWidth");
        bool result = (bool)method.Invoke(null, new object?[] { width })!;
        Assert.Equal(expected, result);
    }

    public static IEnumerable<object[]> CropRangeData => new[]
    {
        new object[]
        {
            (uint)766,
            new (int Start, int End)[]
            {
                (0, 149),
                (154, 303),
                (308, 457),
                (462, 611),
                (616, 765)
            }
        },
        new object[]
        {
            (uint)774,
            new (int Start, int End)[]
            {
                (0, 149),
                (155, 305),
                (311, 461),
                (467, 617),
                (623, 773)
            }
        }
    };

    [Theory]
    [MemberData(nameof(CropRangeData))]
    public void GetCropRanges_ReturnsExpected(uint width, (int Start, int End)[] expected)
    {
        var method = GetMethod("GetCropRanges");
        var result = ((int Start, int End)[])method.Invoke(null, new object?[] { width })!;
        Assert.Equal(expected, result);
    }

    [Fact]
    public void ModifyGifFile_UpdatesHeightAndTail()
    {
        const int adjustedHeight = 0x1234;
        string tempFile = Path.GetTempFileName();

        try
        {
            File.WriteAllBytes(tempFile, SampleGif);
            var method = GetMethod("ModifyGifFile");
            method.Invoke(null, new object?[] { tempFile, adjustedHeight });

            byte[] modified = File.ReadAllBytes(tempFile);
            Assert.Equal(0x21, modified[^1]);

            ushort expected = (ushort)adjustedHeight;
            Assert.Equal((byte)(expected & 0xFF), modified[8]);
            Assert.Equal((byte)(expected >> 8), modified[9]);
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    [Fact]
    public void SynchronizeToShortestDuration_TrimsAndClonesFrames()
    {
        static MagickImageCollection Create(int frames)
        {
            var collection = new MagickImageCollection();
            for (int i = 0; i < frames; i++)
            {
                var img = new MagickImage(MagickColors.Red, 1, 1)
                {
                    AnimationDelay = 10,
                    AnimationTicksPerSecond = 100
                };
                collection.Add(img);
            }
            return collection;
        }

        var shortGif = Create(2);      // 0.2s total
        var longGif = Create(3);       // 0.3s total
        var collections = new[]
        {
            shortGif,
            longGif,
            Create(2),
            Create(2),
            Create(2)
        };

        var method = GetMethod("SynchronizeToShortestDuration");
        var form = new GifToolMainForm();
        var result = (MagickImageCollection[])method.Invoke(null, new object?[] { collections, form })!;

        double expected = shortGif.Sum(f => (double)f.AnimationDelay / f.AnimationTicksPerSecond);
        double duration0 = result[0].Sum(f => (double)f.AnimationDelay / f.AnimationTicksPerSecond);
        double duration1 = result[1].Sum(f => (double)f.AnimationDelay / f.AnimationTicksPerSecond);

        Assert.Equal(expected, duration0, 3); // unchanged
        Assert.Equal(expected, duration1, 3); // trimmed
        Assert.NotSame(shortGif[0], result[0][0]); // frames cloned
        Assert.Equal(2, result[0].Count);
        Assert.Equal(2, result[1].Count); // frame dropped from longGif

        foreach (var col in result)
        {
            col.Dispose();
        }
        foreach (var col in collections)
        {
            col.Dispose();
        }
    }

    private static string CreateGif(string directory, string name, int frames, int delay, MagickColor color)
    {
        string path = Path.Combine(directory, name);
        using var collection = new MagickImageCollection();
        for (int i = 0; i < frames; i++)
        {
            var frame = new MagickImage(color, 1, 1)
            {
                AnimationDelay = (ushort)delay,
                AnimationTicksPerSecond = 100
            };
            collection.Add(frame);
        }
        collection.Write(path);
        return path;
    }

    [Fact]
    public void OverlayGif_OverlayFasterThanBase_UsesOverlayTiming()
    {
        string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        try
        {
            string basePath = CreateGif(tempDir, "base.gif", 2, 10, MagickColors.Red);
            string overlayPath = CreateGif(tempDir, "overlay.gif", 4, 5, MagickColors.Blue);
            string outputPath = Path.Combine(tempDir, "output.gif");

            GifProcessor.OverlayGif(basePath, overlayPath, outputPath);

            using var overlay = new MagickImageCollection(overlayPath);
            overlay.Coalesce();
            using var result = new MagickImageCollection(outputPath);
            result.Coalesce();

            Assert.Equal(overlay.Count, result.Count);
            for (int i = 0; i < overlay.Count; i++)
            {
                Assert.Equal(overlay[i].AnimationDelay, result[i].AnimationDelay);
            }
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void OverlayGif_BaseFasterThanOverlay_UsesOverlayTiming()
    {
        string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        try
        {
            string basePath = CreateGif(tempDir, "base.gif", 4, 5, MagickColors.Red);
            string overlayPath = CreateGif(tempDir, "overlay.gif", 2, 10, MagickColors.Blue);
            string outputPath = Path.Combine(tempDir, "output.gif");

            GifProcessor.OverlayGif(basePath, overlayPath, outputPath);

            using var overlay = new MagickImageCollection(overlayPath);
            overlay.Coalesce();
            using var result = new MagickImageCollection(outputPath);
            result.Coalesce();

            Assert.Equal(overlay.Count, result.Count);
            for (int i = 0; i < overlay.Count; i++)
            {
                Assert.Equal(overlay[i].AnimationDelay, result[i].AnimationDelay);
            }
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }
}

