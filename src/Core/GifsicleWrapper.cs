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

    public static Func<ProcessStartInfo, Task<(int ExitCode, string Output, string Error)>> ProcessRunner = DefaultProcessRunner;

    private static string? _resolvedExecutablePath;

    /// <summary>
    /// Resolves the gifsicle executable. Prefers a copy bundled next to the application
    /// (<c>gifsicle.exe</c> in the base directory) and falls back to <c>gifsicle</c> on PATH.
    /// </summary>
    public static string ResolveExecutablePath()
    {
        if (_resolvedExecutablePath != null) return _resolvedExecutablePath;
        try
        {
            string bundled = Path.Combine(AppContext.BaseDirectory, "gifsicle.exe");
            _resolvedExecutablePath = File.Exists(bundled) ? bundled : "gifsicle";
        }
        catch
        {
            _resolvedExecutablePath = "gifsicle";
        }
        return _resolvedExecutablePath;
    }

    // Builds the shared optimize/colors/lossy/dither argument prefix (trailing space included).
    private static string BuildOptionArgs(GifsicleOptions options)
    {
        StringBuilder argsBuilder = new StringBuilder();
        argsBuilder.Append($"--optimize={options.OptimizeLevel} ");
        argsBuilder.Append($"--colors={options.Colors} ");
        argsBuilder.Append($"--lossy={options.Lossy} ");

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

        return argsBuilder.ToString();
    }

    private static async Task<(int ExitCode, string Output, string Error)> DefaultProcessRunner(ProcessStartInfo startInfo)
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
            catch (Exception ex)
            {
                // Log process kill failure but continue with timeout exception
                System.Diagnostics.Debug.WriteLine($"Failed to kill gifsicle process: {ex.Message}");
            }
            throw new TimeoutException($"The process did not exit within {ProcessTimeout.TotalSeconds} seconds. Command: {startInfo.FileName} {startInfo.Arguments}");
        }

        await Task.WhenAll(outputTask, errorTask);
        return (gifsicleProcess.ExitCode, await outputTask, await errorTask);
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
        string arguments = BuildOptionArgs(options) + $"\"{inputPath}\" -o \"{outputPath}\"";

        ProcessStartInfo startInfo = new ProcessStartInfo
        {
            FileName = ResolveExecutablePath(),
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        progress?.Report(0);

        var processTask = ProcessRunner(startInfo);

        progress?.Report(50);

        var (exitCode, output, error) = await processTask;

        progress?.Report(100);

        // gifsicle writes warnings (e.g. about lossy compression or trailing bytes after the
        // GIF trailer) to stderr even on success, so stderr presence alone is not a failure.
        // Only a non-zero exit code indicates the optimization actually failed.
        if (exitCode != 0)
        {
            string detail = string.IsNullOrWhiteSpace(error) ? output : error;
            throw new Exception($"Gifsicle failed (exit code {exitCode}): {detail}");
        }
    }

    /// <summary>
    /// Optimizes a GIF entirely in memory by piping the bytes through gifsicle's stdin/stdout —
    /// no temporary files are written. Used by the auto-fit search loop, which runs gifsicle many
    /// times against the same in-memory source.
    /// </summary>
    public static async Task<byte[]> OptimizeGifInMemory(byte[] input, GifsicleOptions? options = null)
    {
        if (input == null || input.Length == 0)
            throw new ArgumentException("Input GIF bytes cannot be null or empty.", nameof(input));

        if (options == null) options = new GifsicleOptions();

        // No input/output file arguments → gifsicle reads stdin and writes stdout.
        ProcessStartInfo startInfo = new ProcessStartInfo
        {
            FileName = ResolveExecutablePath(),
            Arguments = BuildOptionArgs(options),
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using Process process = new Process { StartInfo = startInfo };
        process.Start();

        // Start draining stdout (binary) and stderr before writing stdin, so a large output
        // cannot fill gifsicle's stdout pipe and deadlock while we are still feeding stdin.
        Task<byte[]> outputTask = ReadAllBytesAsync(process.StandardOutput.BaseStream);
        Task<string> errorTask = process.StandardError.ReadToEndAsync();

        using (Stream stdin = process.StandardInput.BaseStream)
        {
            await stdin.WriteAsync(input, 0, input.Length);
            await stdin.FlushAsync();
        } // closing stdin signals end-of-input to gifsicle

        using var cts = new CancellationTokenSource(ProcessTimeout);
        try
        {
            await process.WaitForExitAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
            try
            {
                process.Kill(entireProcessTree: true);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to kill gifsicle process: {ex.Message}");
            }
            throw new TimeoutException($"The process did not exit within {ProcessTimeout.TotalSeconds} seconds.");
        }

        byte[] output = await outputTask;
        string error = await errorTask;

        if (process.ExitCode != 0)
        {
            throw new Exception($"Gifsicle failed (exit code {process.ExitCode}): {error}");
        }

        return output;
    }

    private static async Task<byte[]> ReadAllBytesAsync(Stream stream)
    {
        using var memory = new MemoryStream();
        await stream.CopyToAsync(memory);
        return memory.ToArray();
    }
}
