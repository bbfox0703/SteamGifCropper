using System;
using System.Reflection;
using System.Collections.Generic;
using GifProcessorApp;

namespace SteamGifCropper.Tests;

public class GifProcessorTests
{
    private static MethodInfo GetMethod(string name) =>
        typeof(GifProcessor).GetMethod(name, BindingFlags.NonPublic | BindingFlags.Static) ??
        throw new InvalidOperationException($"Method '{name}' not found.");

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
}

