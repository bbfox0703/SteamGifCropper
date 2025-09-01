using System;
using System.IO;
using ImageMagick;
using Xunit;

namespace SteamGifCropper.Tests;

public class LargeGifMemoryTests
{
    [Fact]
    public void LargeGif_RespectsResourceLimits()
    {
        string tempDir = Directory.CreateTempSubdirectory().FullName;
        string gifPath = Path.Combine(tempDir, "large.gif");

        using (var collection = new MagickImageCollection())
        {
            for (int i = 0; i < 50; i++)
            {
                var img = new MagickImage(MagickColors.Red, 500, 500);
                img.AnimationDelay = 1;
                collection.Add(img);
            }
            collection.Write(gifPath);
        }

        ulong originalMemory = ResourceLimits.Memory;
        ulong originalDisk = ResourceLimits.Disk;
        try
        {
            ResourceLimits.Memory = 8UL * 1024UL * 1024UL; // 8 MB
            ResourceLimits.Disk = 256UL * 1024UL * 1024UL; // 256 MB

            using var loaded = new MagickImageCollection(gifPath);
            loaded.Coalesce();
            Assert.Equal(50, loaded.Count);
        }
        finally
        {
            ResourceLimits.Memory = originalMemory;
            ResourceLimits.Disk = originalDisk;
            Directory.Delete(tempDir, true);
        }
    }
}
