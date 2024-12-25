using System;
using System.Diagnostics;
using System.IO;
using System.Text;

public class GifsicleWrapper
{
    public class GifsicleOptions
    {
        public int Colors { get; set; } = 256;
        public int Lossy { get; set; } = 0;
        public int OptimizeLevel { get; set; } = 1;
        public int Dither { get; set; } = 0;
    }

    public static void OptimizeGif(string inputPath, string outputPath, GifsicleOptions options = null)
    {
        if (options == null) options = new GifsicleOptions();

        try
        {
            // Validate paths
            if (!File.Exists(inputPath))
                throw new FileNotFoundException("Input file does not exist.", inputPath);

            if (string.IsNullOrWhiteSpace(outputPath))
                throw new ArgumentException("Output path cannot be null or empty.");

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

            // Prepare and start the process
            using (Process gifsicleProcess = new Process())
            {
                gifsicleProcess.StartInfo = new ProcessStartInfo
                {
                    FileName = "gifsicle", // ensure gifsicle is in system PATH
                    Arguments = argsBuilder.ToString(),
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                gifsicleProcess.Start();

                string output = gifsicleProcess.StandardOutput.ReadToEnd();
                string error = gifsicleProcess.StandardError.ReadToEnd();

                gifsicleProcess.WaitForExit();

                if (!string.IsNullOrEmpty(error))
                {
                    throw new Exception($"Gifsicle Error: {error}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error optimizing GIF: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
        }
    }
}
