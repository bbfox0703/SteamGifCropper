using System;
using System.Diagnostics;
using System.Windows.Forms;

public class GifsicleWrapper
{
    public static void OptimizeGif(string inputPath, string outputPath, int colors = 256, int lossy = 0, int optim_level = 1, int dither = 0)
    {
        try
        {
            // construct Gifsicle command
            string args = args = $"--optimize={optim_level} --colors={colors} --lossy={lossy} \"{inputPath}\" -o \"{outputPath}\""; ;
            if (dither == 0)
            {
                args = $"--optimize={optim_level} --colors={colors} --lossy={lossy} \"{inputPath}\" -o \"{outputPath}\"";
            }
            else if (dither == 1)
            {
                args = $"--optimize={optim_level} --colors={colors} --lossy={lossy} --dither=ro64 \"{inputPath}\" -o \"{outputPath}\"";
            }
            else if (dither == 2)
            {
                args = $"--optimize={optim_level} --colors={colors} --lossy={lossy} --dither=o8 \"{inputPath}\" -o \"{outputPath}\"";
            }
            else if (dither == 3)
            {
                args = $"--optimize={optim_level} --colors={colors} --lossy={lossy} -f \"{inputPath}\" -o \"{outputPath}\"";
            }
            //MessageBox.Show(args, "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
            Process gifsicleProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "gifsicle", // ensure gifsicle in system path
                    Arguments = args,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
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
        catch (Exception ex)
        {
            Console.WriteLine($"Error optimizing GIF: {ex.Message}");
        }
    }
}
