using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

#nullable enable

public class GifsicleWrapper
{
    public class GifsicleOptions
    {
        public int Colors { get; set; } = 256;
        public int Lossy { get; set; } = 0;
        public int OptimizeLevel { get; set; } = 1;
        public int Dither { get; set; } = 0;
    }

    public static TimeSpan ProcessTimeout { get; set; } = TimeSpan.FromSeconds(30);

    public static Func<ProcessStartInfo, Task<(string Output, string Error)>> ProcessRunner = DefaultProcessRunner;

    private static async Task<(string Output, string Error)> DefaultProcessRunner(ProcessStartInfo startInfo)
    {
        using Process gifsicleProcess = new Process { StartInfo = startInfo };
        gifsicleProcess.Start();

        Task<string> outputTask = gifsicleProcess.StandardOutput.ReadToEndAsync();
        Task<string> errorTask = gifsicleProcess.StandardError.ReadToEndAsync();

        using var cts = new CancellationTokenSource(ProcessTimeout);
        try
        {
            await gifsicleProcess.WaitForExitAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
            try
            {
                gifsicleProcess.Kill(entireProcessTree: true);
            }
            catch { }
            throw new TimeoutException($"The process did not exit within {ProcessTimeout.TotalSeconds} seconds. Command: {startInfo.FileName} {startInfo.Arguments}");
        }

        await Task.WhenAll(outputTask, errorTask);
        return (await outputTask, await errorTask);
    }

    public static async Task OptimizeGif(string inputPath, string outputPath, GifsicleOptions? options = null, IProgress<int>? progress = null)
    {
        if (options == null) options = new GifsicleOptions();

        // Validate paths
        if (!File.Exists(inputPath))
            throw new FileNotFoundException("Input file does not exist.", inputPath);

        if (string.IsNullOrWhiteSpace(outputPath))
            throw new ArgumentException("Output path cannot be null or empty.", nameof(outputPath));

        // Construct command arguments
        StringBuilder argsBuilder = new StringBuilder();
        argsBuilder.Append($"--optimize={options.OptimizeLevel} ");
        argsBuilder.Append($"--colors={options.Colors} ");
        argsBuilder.Append($"--lossy={options.Lossy} ");

        // Add dither options
        switch (options.Dither)
        {
            case 1:
                argsBuilder.Append("--dither=ro64 ");
                break;
            case 2:
                argsBuilder.Append("--dither=o8 ");
                break;
            case 3:
                argsBuilder.Append("-f ");
                break;
        }

        argsBuilder.Append($"\"{inputPath}\" -o \"{outputPath}\"");

        ProcessStartInfo startInfo = new ProcessStartInfo
        {
            FileName = "gifsicle", // ensure gifsicle is in system PATH
            Arguments = argsBuilder.ToString(),
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        progress?.Report(0);

        var processTask = ProcessRunner(startInfo);

        progress?.Report(50);

        var (output, error) = await processTask;

        progress?.Report(100);

        if (!string.IsNullOrEmpty(error))
        {
            throw new Exception($"Gifsicle Error: {error}");
        }
    }
}
