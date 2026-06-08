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
        private static string GenerateOutputPath(string inputPath, string suffix)
        {
            string directory = Path.GetDirectoryName(inputPath);
            string fileName = Path.GetFileNameWithoutExtension(inputPath);
            string extension = Path.GetExtension(inputPath);
            return Path.Combine(directory, $"{fileName}{suffix}{extension}");
        }
        public static void RestoreTailByteForMultipleGifs(GifToolMainForm mainForm)
        {
            using (var openFileDialog = new OpenFileDialog
            {
                Filter = SteamGifCropper.Properties.Resources.FileDialog_GifFilter,
                Title = SteamGifCropper.Properties.Resources.FileDialog_SelectGifRestore,
                Multiselect = true
            })
            {
                if (openFileDialog.ShowDialog() != DialogResult.OK) return;

                string[] selectedFiles = openFileDialog.FileNames;
                ImageInputValidator.ValidateGifs(selectedFiles);
                int processedCount = 0;
                int skippedCount = 0;

                mainForm.Enabled = false;
                try
                {
                    SetStatusText(mainForm, SteamGifCropper.Properties.Resources.Status_RestoringTailBytes);
                    SetProgressBar(mainForm.pBarTaskStatus, 0, mainForm.pBarTaskStatus.Maximum);
                    mainForm.pBarTaskStatus.Maximum = 100;
                    mainForm.pBarTaskStatus.Visible = true;

                    int progress = 0;
                    foreach (string filePath in selectedFiles)
                    {
                        try
                        {
                            if (RestoreGifTailByte(filePath))
                            {
                                processedCount++;
                            }
                            else
                            {
                                skippedCount++;
                            }
                        }
                        catch (Exception ex)
                        {
                            WindowsThemeManager.ShowThemeAwareMessageBox(mainForm,
                                string.Format(SteamGifCropper.Properties.Resources.Error_ProcessingFile,
                                              Path.GetFileName(filePath), ex.Message),
                                SteamGifCropper.Properties.Resources.Title_FileProcessingError,
                                MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            skippedCount++;
                        }

                        progress++;
                        SetProgressBar(mainForm.pBarTaskStatus, progress, selectedFiles.Length);
                        if (progress % ProgressUpdateInterval == 0 || progress == selectedFiles.Length)
                        {
                            SetStatusText(mainForm, string.Format(
                                "Restoring tail bytes {0}/{1}: {2}",
                                progress, selectedFiles.Length, Path.GetFileName(filePath)));
                        }
                    }

                    string resultMessage = string.Format(
                        SteamGifCropper.Properties.Resources.Message_RestorationComplete,
                        processedCount, skippedCount)
                        .Replace("\n", Environment.NewLine);

                    WindowsThemeManager.ShowThemeAwareMessageBox(mainForm,
                                    resultMessage,
                                    SteamGifCropper.Properties.Resources.Title_TailByteRestoration,
                                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    WindowsThemeManager.ShowThemeAwareMessageBox(mainForm,
                                  string.Format(SteamGifCropper.Properties.Resources.Error_Occurred, ex.Message),
                                  SteamGifCropper.Properties.Resources.Title_Error, MessageBoxButtons.OK, MessageBoxIcon.Error);
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

        private static bool RestoreGifTailByte(string filePath)
        {
            try
            {
                byte[] fileData = File.ReadAllBytes(filePath);
                
                if (fileData.Length == 0)
                {
                    return false;
                }

                // Check if the last byte is 0x21
                if (fileData[fileData.Length - 1] != 0x21)
                {
                    // File doesn't have 0x21 as last byte, skip it
                    return false;
                }

                // Change 0x21 to 0x3B
                fileData[fileData.Length - 1] = 0x3B;
                
                File.WriteAllBytes(filePath, fileData);
                return true;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to restore tail byte for {filePath}: {ex.Message}", ex);
            }
        }

        public static void WriteTailByteForMultipleGifs(GifToolMainForm mainForm)
        {
            using (var openFileDialog = new OpenFileDialog
            {
                Filter = SteamGifCropper.Properties.Resources.FileDialog_GifFilter,
                Title = SteamGifCropper.Properties.Resources.FileDialog_SelectGifFiles,
                Multiselect = true
            })
            {
                if (openFileDialog.ShowDialog() != DialogResult.OK) return;

                string[] filePaths = openFileDialog.FileNames;
                ImageInputValidator.ValidateGifs(filePaths);
                int processedFiles = 0;

                mainForm.Enabled = false;
                try
                {
                    mainForm.pBarTaskStatus.Visible = true;
                    SetProgressBar(mainForm.pBarTaskStatus, 0, mainForm.pBarTaskStatus.Maximum);
                    mainForm.pBarTaskStatus.Maximum = 100;

                    foreach (string filePath in filePaths)
                    {
                        try
                        {
                            // Update status with current file being processed
                            SetStatusText(mainForm, string.Format(
                                "Modifying tail bytes {0}/{1}: {2}",
                                processedFiles + 1,
                                filePaths.Count(),
                                Path.GetFileName(filePath)));
                                
                            if (ProcessTailByte(filePath))
                                processedFiles++;

                            SetProgressBar(mainForm.pBarTaskStatus, processedFiles, filePaths.Length);
                        }
                        catch (Exception ex)
                        {
                            WindowsThemeManager.ShowThemeAwareMessageBox(
                                mainForm,
                                string.Format(SteamGifCropper.Properties.Resources.Error_ProcessingFile,
                                              Path.GetFileName(filePath), ex.Message),
                                SteamGifCropper.Properties.Resources.Title_FileProcessingError,
                                MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }
                    }

                    WindowsThemeManager.ShowThemeAwareMessageBox(
                        mainForm,
                        string.Format(SteamGifCropper.Properties.Resources.Message_ProcessedFiles,
                                      processedFiles, filePaths.Length),
                        SteamGifCropper.Properties.Resources.Title_Success,
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    WindowsThemeManager.ShowThemeAwareMessageBox(mainForm, string.Format(SteamGifCropper.Properties.Resources.Error_Occurred, ex.Message),
                                    SteamGifCropper.Properties.Resources.Title_Error, MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                finally
                {
                    mainForm.Enabled = true;
                    SetProgressBar(mainForm.pBarTaskStatus, 0, mainForm.pBarTaskStatus.Maximum);
                    //mainForm.pBarTaskStatus.Visible = false;
                    SetStatusText(mainForm, SteamGifCropper.Properties.Resources.Status_Idle);
                }
            }
        }

        private static bool ProcessTailByte(string filePath)
        {
            const byte gifTrailer = 0x3B;
            const byte modifiedTrailer = 0x21;

            byte[] fileData = File.ReadAllBytes(filePath);
            if (fileData.Length == 0) return false;

            if (fileData[fileData.Length - 1] == gifTrailer)
            {
                fileData[fileData.Length - 1] = modifiedTrailer;
                File.WriteAllBytes(filePath, fileData);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Flips the trailing byte of any GIF that ends with the Steam trailer (0x21) back to the
        /// standard GIF trailer (0x3B) so ImageMagick can decode it. Returns the list of files that
        /// were changed; the caller must restore them with <see cref="RestoreSteamTail"/> when done.
        /// If a file cannot be rewritten, every file already flipped in this call is rolled back and
        /// the original exception (which names the offending file) is rethrown.
        /// </summary>
        private static List<string> FlipSteamTailToStandard(IEnumerable<string> gifPaths)
        {
            const byte gifTrailer = 0x3B;
            const byte steamTrailer = 0x21;

            var flipped = new List<string>();
            try
            {
                foreach (string path in gifPaths)
                {
                    byte[] data = File.ReadAllBytes(path);
                    if (data.Length > 0 && data[data.Length - 1] == steamTrailer)
                    {
                        data[data.Length - 1] = gifTrailer;
                        File.WriteAllBytes(path, data);
                        flipped.Add(path);
                    }
                }
                return flipped;
            }
            catch
            {
                // Best-effort rollback so a failed pre-pass leaves the sources untouched.
                RestoreSteamTail(flipped);
                throw;
            }
        }

        /// <summary>
        /// Restores the Steam trailer byte (0x3B -> 0x21) on the given files. Best-effort: returns
        /// the list of files that could NOT be restored (an empty list means everything succeeded).
        /// </summary>
        private static List<string> RestoreSteamTail(IEnumerable<string> paths)
        {
            const byte gifTrailer = 0x3B;
            const byte steamTrailer = 0x21;

            var failed = new List<string>();
            if (paths == null) return failed;

            foreach (string path in paths)
            {
                try
                {
                    byte[] data = File.ReadAllBytes(path);
                    if (data.Length > 0 && data[data.Length - 1] == gifTrailer)
                    {
                        data[data.Length - 1] = steamTrailer;
                        File.WriteAllBytes(path, data);
                    }
                }
                catch
                {
                    failed.Add(path);
                }
            }
            return failed;
        }

    }
}
