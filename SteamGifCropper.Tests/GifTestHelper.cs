using System;
using System.IO;
using ImageMagick;

namespace SteamGifCropper.Tests;

internal static class GifTestHelper
{
    public static string CreateGradientGif(string directory, int width, int height, int frames, string startColor, string endColor)
    {
        string path = Path.Combine(directory, Guid.NewGuid().ToString() + ".gif");
        using var collection = new MagickImageCollection();
        for (int i = 0; i < frames; i++)
        {
            var settings = new MagickReadSettings { Width = (uint)width, Height = (uint)height };
            var image = new MagickImage($"gradient:{startColor}-{endColor}", settings);
            image.AnimationDelay = 10;
            image.AnimationTicksPerSecond = 100;
            collection.Add(image);
        }
        collection.Write(path);
        return path;
    }
}
