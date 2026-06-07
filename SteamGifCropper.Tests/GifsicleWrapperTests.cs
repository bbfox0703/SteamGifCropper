using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

public class GifsicleWrapperTests
{
    public static IEnumerable<object[]> DitherData => new[]
    {
        new object[] { 1, "--dither=ro64" },
        new object[] { 2, "--dither=o8" },
        new object[] { 3, "-f" }
    };

    [Theory]
    [MemberData(nameof(DitherData))]
    public async Task OptimizeGif_BuildsExpectedArguments(int dither, string expectedFlag)
    {
        string input = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".gif");
        string output = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".gif");
        File.WriteAllBytes(input, Array.Empty<byte>());

        var originalRunner = GifsicleWrapper.ProcessRunner;
        ProcessStartInfo? captured = null;
        try
        {
            GifsicleWrapper.ProcessRunner = psi =>
            {
                captured = psi;
                return Task.FromResult((ExitCode: 0, Output: "", Error: ""));
            };

            var options = new GifsicleWrapper.GifsicleOptions
            {
                OptimizeLevel = 3,
                Colors = 128,
                Lossy = 80,
                Dither = dither
            };

            await GifsicleWrapper.OptimizeGif(input, output, options);

            Assert.NotNull(captured);
            string args = captured!.Arguments;
            Assert.Contains("--optimize=3", args);
            Assert.Contains("--colors=128", args);
            Assert.Contains("--lossy=80", args);
            Assert.Contains(expectedFlag, args);
        }
        finally
        {
            GifsicleWrapper.ProcessRunner = originalRunner;
            if (File.Exists(input)) File.Delete(input);
            if (File.Exists(output)) File.Delete(output);
        }
    }

    [Fact]
    public async Task OptimizeGif_NonexistentInput_Throws()
    {
        string input = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName(), "missing.gif");
        await Assert.ThrowsAsync<FileNotFoundException>(() => GifsicleWrapper.OptimizeGif(input, "out.gif"));
    }

    [Fact]
    public async Task OptimizeGifInMemory_PipesValidSmallerGif()
    {
        // Exercises the binary stdin/stdout pipe against the bundled gifsicle (copied to the test
        // output dir). Aggressive colors/lossy on a 1920x1080 sample must yield a valid, smaller GIF.
        byte[] input = await File.ReadAllBytesAsync(Path.Combine("TestData", "test4_1920x1080_10s.gif"));
        var options = new GifsicleWrapper.GifsicleOptions
        {
            OptimizeLevel = 3,
            Colors = 32,
            Lossy = 120,
            Dither = 0
        };

        byte[] output = await GifsicleWrapper.OptimizeGifInMemory(input, options);

        Assert.NotNull(output);
        Assert.True(output.Length > 6, "output should be a non-trivial GIF");
        // GIF signature "GIF8"
        Assert.Equal((byte)'G', output[0]);
        Assert.Equal((byte)'I', output[1]);
        Assert.Equal((byte)'F', output[2]);
        Assert.Equal((byte)'8', output[3]);
        Assert.True(output.Length < input.Length, "aggressive settings should shrink the GIF");
    }

    [Fact]
    public async Task OptimizeGifInMemory_EmptyInput_Throws()
    {
        await Assert.ThrowsAsync<ArgumentException>(() => GifsicleWrapper.OptimizeGifInMemory(Array.Empty<byte>()));
    }
}
