using FFMpegCore;
using FFMpegCore.Exceptions;
using FFMpegCore.Pipes;
using ImageMagick;
using ImageMagick.Drawing;
using SteamGifCropper;
using SteamGifCropper.Properties;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GifProcessorApp
{
    public static partial class GifProcessor
    {
        public static async Task ConvertMp4ToGif(GifToolMainForm mainForm)
        {
            // Check if FFmpeg is available with detailed diagnostics
            var (isAvailable, ffmpegPath, ffmpegVersion, error) = GetFFmpegDiagnostics();
            
            if (!isAvailable)
            {
                string errorPart = string.IsNullOrEmpty(error) ? string.Empty : $"Error: {error}\n";
                string diagMessage = string.Format(SteamGifCropper.Properties.Resources.Mp4ToGif_FFmpegRequiredMessage,
                                                   ffmpegPath ?? "Not found",
                                                   ffmpegVersion ?? "N/A",
                                                   errorPart);
                WindowsThemeManager.ShowThemeAwareMessageBox(mainForm,
                                diagMessage,
                                SteamGifCropper.Properties.Resources.Title_FFmpegRequired,
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Warning);
                return;
            }

            // Show MP4 to GIF conversion dialog
            using (var conversionDialog = new Mp4ToGifDialog())
            {
                if (conversionDialog.ShowDialog() != DialogResult.OK)
                    return;

                // Get conversion parameters
                var inputPath = conversionDialog.InputFilePath;
                var outputPath = conversionDialog.OutputFilePath;
                var startTime = conversionDialog.StartTime;
                var duration = conversionDialog.Duration;
                var targetFramerate = (int)mainForm.numUpDownFramerate.Value;

                mainForm.Enabled = false;
                try
                {
                    mainForm.pBarTaskStatus.Visible = true;
                    SetProgressBar(mainForm.pBarTaskStatus, 0, mainForm.pBarTaskStatus.Maximum);
                    
                    SetStatusText(mainForm, "Analyzing video...");
                    SetProgressBar(mainForm.pBarTaskStatus, 10, mainForm.pBarTaskStatus.Maximum);
                    await Task.Delay(100);
                    
                    SetStatusText(mainForm, "Generating optimal color palette...");
                    SetProgressBar(mainForm.pBarTaskStatus, 30, mainForm.pBarTaskStatus.Maximum);
                    await Task.Delay(100);
                    
                    SetStatusText(mainForm, "Converting video to GIF...");
                    SetProgressBar(mainForm.pBarTaskStatus, 50, mainForm.pBarTaskStatus.Maximum);
                    await Task.Delay(100);
                    
                    await ProcessWithOptimizedCpu(inputPath, outputPath, startTime, duration, targetFramerate);

                    SetProgressBar(mainForm.pBarTaskStatus, 100, mainForm.pBarTaskStatus.Maximum);
                    SetStatusText(mainForm, SteamGifCropper.Properties.Resources.Mp4ToGif_Success);
                    
                    WindowsThemeManager.ShowThemeAwareMessageBox(mainForm,
                                  string.Format(SteamGifCropper.Properties.Resources.Mp4ToGif_SuccessMessage, Path.GetFileName(outputPath)),
                                  SteamGifCropper.Properties.Resources.Title_Success, MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    string detailedError = ex.ToString();
                    string userFriendlyMessage;

                    // Capture detailed FFmpeg output if available
                    string ffmpegOutput = null;
                    string logFilePath = null;
                    if (ex is FFMpegException ffmpegException && !string.IsNullOrWhiteSpace(ffmpegException.FFMpegErrorOutput))
                    {
                        ffmpegOutput = ffmpegException.FFMpegErrorOutput;
                        try
                        {
                            string logDirectory = Path.GetDirectoryName(outputPath);
                            if (string.IsNullOrEmpty(logDirectory) || !Directory.Exists(logDirectory))
                                logDirectory = Path.GetTempPath();

                            logFilePath = Path.Combine(logDirectory, "ffmpeg_error.log");
                            File.WriteAllText(logFilePath, ffmpegOutput);
                        }
                        catch
                        {
                            // Ignore logging failures
                        }
                    }

                    if (ex.Message.Contains("No such file or directory") || ex.Message.Contains("not found"))
                    {
                        userFriendlyMessage = SteamGifCropper.Properties.Resources.Mp4ToGif_Error_FFmpegNotFound;
                    }
                    else if (ex.Message.Contains("Invalid data found") || ex.Message.Contains("moov atom not found"))
                    {
                        userFriendlyMessage = SteamGifCropper.Properties.Resources.Mp4ToGif_Error_CorruptedInput;
                    }
                    else if (ex.Message.Contains("Permission denied") || ex.Message.Contains("Access is denied"))
                    {
                        userFriendlyMessage = SteamGifCropper.Properties.Resources.Mp4ToGif_Error_PermissionDenied;
                    }
                    else
                    {
                        userFriendlyMessage = SteamGifCropper.Properties.Resources.Mp4ToGif_Error_Unexpected;
                    }

                    // Append FFmpeg stderr details
                    if (!string.IsNullOrEmpty(ffmpegOutput))
                    {
                        if (!string.IsNullOrEmpty(logFilePath))
                        {
                            userFriendlyMessage += string.Format(SteamGifCropper.Properties.Resources.Mp4ToGif_Error_DetailsSaved, logFilePath);
                        }
                        else
                        {
                            string truncated = ffmpegOutput.Length > 500 ? ffmpegOutput.Substring(0, 500) + "..." : ffmpegOutput;
                            userFriendlyMessage += string.Format(SteamGifCropper.Properties.Resources.Mp4ToGif_Error_FFmpegOutputTruncated, truncated);
                        }
                    }

                    WindowsThemeManager.ShowThemeAwareMessageBox(mainForm,
                                                  string.Format(SteamGifCropper.Properties.Resources.Mp4ToGif_ErrorMessageDetails,
                                                  userFriendlyMessage, inputPath, outputPath, startTime, duration, ex.Message),
                                  SteamGifCropper.Properties.Resources.Title_Mp4ToGifError, MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                finally
                {
                    mainForm.Enabled = true;
                    SetProgressBar(mainForm.pBarTaskStatus, 0, mainForm.pBarTaskStatus.Maximum);
                    //mainForm.pBarTaskStatus.Visible = false;
                    SetStatusText(mainForm, SteamGifCropper.Properties.Resources.Status_Ready);
                }
            }
        }

        private static bool IsFFmpegAvailable()
        {
            var (isAvailable, _, _, _) = GetFFmpegDiagnostics();
            return isAvailable;
        }

        private static async Task ProcessWithOptimizedCpu(string inputPath, string outputPath, TimeSpan startTime, TimeSpan duration, int targetFramerate = 25)
        {
            // Use direct file input/output instead of pipes for better compatibility
            try
            {
                var token = CreateFfmpegCancellationToken();
                await FFMpegArguments
                    .FromFileInput(inputPath)
                    .OutputToFile(outputPath, true, options =>
                    {
                        options.ForceFormat("gif");

                        // Only apply seek if startTime is greater than 0
                        if (startTime > TimeSpan.Zero)
                        {
                            options.Seek(startTime);
                        }

                        // Only apply duration if it's reasonable (not too short)
                        if (duration > TimeSpan.FromSeconds(0.1))
                        {
                            options.WithDuration(duration);
                        }

                        options.WithFramerate(targetFramerate)
                               .WithCustomArgument("-pix_fmt rgb8")
                               .WithCustomArgument("-an");
                        ApplyThreadLimit(options);
                    })
                    .CancellableThrough(token)
                    .ProcessAsynchronously();
            }
            catch (FFMpegException ex) when (ex.FFMpegErrorOutput?.Contains("partial file") == true ||
                                           ex.FFMpegErrorOutput?.Contains("Invalid argument") == true ||
                                           ex.FFMpegErrorOutput?.Contains("Error during demuxing") == true)
            {
                // Retry without seek and duration parameters if file reading fails
                // This handles cases where the video file format or codec has issues with seeking
                // Create a new CancellationToken for the retry attempt
                var retryToken = CreateFfmpegCancellationToken();
                await FFMpegArguments
                    .FromFileInput(inputPath)
                    .OutputToFile(outputPath, true, options =>
                    {
                        options.ForceFormat("gif")
                               .WithFramerate(targetFramerate)
                               .WithCustomArgument("-pix_fmt rgb8")
                               .WithCustomArgument("-an")
                               .WithCustomArgument("-avoid_negative_ts make_zero"); // Handle timing issues
                        ApplyThreadLimit(options);
                    })
                    .CancellableThrough(retryToken)
                    .ProcessAsynchronously();
            }
        }

        private static (bool isAvailable, string ffmpegPath, string version, string error) GetFFmpegDiagnostics()
        {
            try
            {
                // First, try to find FFmpeg in PATH
                string ffmpegPath = "ffmpeg"; // Will use PATH lookup

                using var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = ffmpegPath,
                        Arguments = "-version",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();

                bool finished = process.WaitForExit(5000); // Wait max 5 seconds

                if (!finished)
                {
                    try
                    {
                        process.Kill();
                    }
                    catch (Exception ex)
                    {
                        // Log process kill failure but don't throw
                        System.Diagnostics.Debug.WriteLine($"Failed to kill FFmpeg process: {ex.Message}");
                    }
                    return (false, ffmpegPath, null, "FFmpeg process timed out");
                }

                if (process.ExitCode == 0 && (output.Contains("ffmpeg version") || output.Contains("configuration:")))
                {
                    // Extract version from output
                    string version = "Unknown";
                    var lines = output.Split('\n');
                    if (lines.Length > 0 && lines[0].Contains("ffmpeg version"))
                    {
                        version = lines[0].Trim();
                    }

                    return (true, ffmpegPath, version, null);
                }
                else
                {
                    return (false, ffmpegPath, null, $"Exit code: {process.ExitCode}, Output: {output}, Error: {error}");
                }
            }
            catch (Exception ex)
            {
                return (false, null, null, ex.Message);
            }
        }

        public static async Task ReverseGif(GifToolMainForm mainForm)
        {
            using (var openFileDialog = new OpenFileDialog
            {
                Filter = SteamGifCropper.Properties.Resources.FileDialog_GifFilter,
                Title = SteamGifCropper.Properties.Resources.FileDialog_SelectGifToReverse ?? "Select a GIF file to reverse"
            })
            {
                if (openFileDialog.ShowDialog() != DialogResult.OK) return;

                string inputFilePath = openFileDialog.FileName;
                ImageInputValidator.ValidateGif(inputFilePath);
                string inputFileName = Path.GetFileNameWithoutExtension(inputFilePath);
                string inputDirectory = Path.GetDirectoryName(inputFilePath);
                string outputFilePath = Path.Combine(inputDirectory, $"{inputFileName}_reversed.gif");

                using (var saveFileDialog = new SaveFileDialog
                {
                    Filter = SteamGifCropper.Properties.Resources.FileDialog_GifFilter,
                    Title = SteamGifCropper.Properties.Resources.FileDialog_SaveReversedGif ?? "Save reversed GIF as",
                    FileName = Path.GetFileName(outputFilePath)
                })
                {
                    if (saveFileDialog.ShowDialog() != DialogResult.OK) return;
                    outputFilePath = saveFileDialog.FileName;
                }

                SetStatusText(mainForm, SteamGifCropper.Properties.Resources.Status_ReversingGif ?? "Reversing GIF...");
                mainForm.pBarTaskStatus.Visible = true;
                SetProgressBar(mainForm.pBarTaskStatus, 0, mainForm.pBarTaskStatus.Maximum);

                mainForm.Enabled = false;
                try
                {
                    // Check FFmpeg availability
                    var (isAvailable, ffmpegPath, version, error) = GetFFmpegDiagnostics();
                    if (!isAvailable)
                    {
                        throw new InvalidOperationException($"FFmpeg not available: {error}");
                    }

                    // Get target framerate from main form
                    int targetFramerate = (int)mainForm.numUpDownFramerate.Value;

                    SetProgressBar(mainForm.pBarTaskStatus, 25, mainForm.pBarTaskStatus.Maximum);
                    await Task.Delay(1);

                    // Use FFMpegCore to reverse the GIF
                    var inputAnalysis = await FFProbe.AnalyseAsync(inputFilePath);
                    var totalDuration = inputAnalysis.Duration;

                    SetProgressBar(mainForm.pBarTaskStatus, 50, mainForm.pBarTaskStatus.Maximum);
                    await Task.Delay(1);
                    // Reverse GIF directly with palettegen + paletteuse using streaming to limit memory usage
                    SetProgressBar(mainForm.pBarTaskStatus, 75, mainForm.pBarTaskStatus.Maximum);
                    await using var reverseInput = File.OpenRead(inputFilePath);
                    await using var reverseOutput = File.Open(outputFilePath, FileMode.Create, FileAccess.Write);
                    var token = CreateFfmpegCancellationToken();
                    await FFMpegArguments
                        .FromPipeInput(new StreamPipeSource(reverseInput))
                        .OutputToPipe(new StreamPipeSink(reverseOutput), options =>
                            {
                                options.ForceFormat("gif")
                                       .WithCustomArgument(
                                           @"-filter_complex ""[0:v]reverse,split[s0][s1];[s0]palettegen=stats_mode=single[p];[s1][p]paletteuse=dither=bayer:bayer_scale=3"""
                                       )
                                       .WithFramerate(targetFramerate);
                                ApplyThreadLimit(options);
                            })
                        .CancellableThrough(token)
                        .ProcessAsynchronously();

                    SetProgressBar(mainForm.pBarTaskStatus, 100, mainForm.pBarTaskStatus.Maximum);
                    await Task.Delay(1);

                    SetStatusText(mainForm, SteamGifCropper.Properties.Resources.Status_GifReversed ?? "GIF reversed successfully!");
                    WindowsThemeManager.ShowThemeAwareMessageBox(
                        mainForm,
                        (SteamGifCropper.Properties.Resources.Message_GifReversedSuccess ?? "GIF reversed successfully!") + $"\n{outputFilePath}",
                        SteamGifCropper.Properties.Resources.Title_Success ?? "Success",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    // Fallback to ImageMagick if FFmpeg fails
                    try
                    {
                        SetStatusText(mainForm, SteamGifCropper.Properties.Resources.Status_FFmpegFallback);
                        SetProgressBar(mainForm.pBarTaskStatus, 25, 100);

                        // Get target framerate from main form (UI thread, before Task.Run)
                        int fallbackFramerate = (int)mainForm.numUpDownFramerate.Value;

                        await Task.Run(() =>
                        {
                            using (var collection = new MagickImageCollection(inputFilePath))
                            {
                                SetProgressBar(mainForm.pBarTaskStatus, 50, 100);

                                // Reverse the frame order
                                collection.Reverse();

                                SetProgressBar(mainForm.pBarTaskStatus, 75, 100);

                                // Apply framerate setting to all frames
                                uint frameDelay = (uint)(100.0 / fallbackFramerate); // Convert fps to delay (in 1/100th seconds)
                                foreach (var frame in collection)
                                {
                                    frame.AnimationDelay = frameDelay;
                                }

                                SetProgressBar(mainForm.pBarTaskStatus, 90, 100);

                                collection.Write(outputFilePath);

                                SetProgressBar(mainForm.pBarTaskStatus, 100, 100);
                            }
                        });

                        SetStatusText(mainForm, SteamGifCropper.Properties.Resources.Status_GifReversed ?? "GIF reversed successfully!");
                        WindowsThemeManager.ShowThemeAwareMessageBox(
                            mainForm,
                            (SteamGifCropper.Properties.Resources.Message_GifReversedSuccess ?? "GIF reversed successfully!") + $"\n{outputFilePath}",
                            SteamGifCropper.Properties.Resources.Title_Success ?? "Success",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information);
                    }
                    catch (Exception fallbackEx)
                    {
                        SetStatusText(mainForm, SteamGifCropper.Properties.Resources.Status_Error ?? "Error");
                        WindowsThemeManager.ShowThemeAwareMessageBox(
                            mainForm,
                            string.Format(SteamGifCropper.Properties.Resources.Error_GifReverseFailed ?? "Failed to reverse GIF: {0}", $"FFmpeg: {ex.Message}, ImageMagick: {fallbackEx.Message}"),
                            SteamGifCropper.Properties.Resources.Title_Error ?? "Error",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error);
                    }
                }
                finally
                {
                    mainForm.Enabled = true;
                    SetProgressBar(mainForm.pBarTaskStatus, 0, mainForm.pBarTaskStatus.Maximum);
                    //mainForm.pBarTaskStatus.Visible = false;
                    SetStatusText(mainForm, SteamGifCropper.Properties.Resources.Status_Ready ?? "Ready");
                }
            }
        }

    }
}
