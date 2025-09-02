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
                return Task.FromResult((Output: "", Error: ""));
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
}
