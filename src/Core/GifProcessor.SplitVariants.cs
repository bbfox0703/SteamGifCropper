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
        public static void SplitGif(string inputFilePath, string outputDirectory)
        {
            ImageInputValidator.ValidateGif(inputFilePath);
            using var collection = new MagickImageCollection(inputFilePath);
            collection.Coalesce();

            uint canvasWidth = collection[0].Width;
            if (!IsValidCanvasWidth(canvasWidth))
            {
                throw new InvalidOperationException($"Unsupported width: {canvasWidth}");
            }

            var ranges = GetCropRanges(canvasWidth);
            int canvasHeight = (int)collection[0].Height;
            int newHeight = canvasHeight + HeightExtension;
            Directory.CreateDirectory(outputDirectory);

            var (recalculatedDelays, ticksPerSecond) = RecalculateGifDelays(collection);
            collection[0].AnimationTicksPerSecond = ticksPerSecond;

            for (int i = 0; i < ranges.Length; i++)
            {
                using var partCollection = new MagickImageCollection();
                for (int frameIndex = 0; frameIndex < collection.Count; frameIndex++)
                {
                    var frame = collection[frameIndex];
                    int copyWidth = ranges[i].End - ranges[i].Start + 1;

                    using var newImage = new MagickImage(MagickColors.Transparent, (uint)copyWidth, (uint)newHeight);
                    var cropGeometry = new MagickGeometry(ranges[i].Start, 0, (uint)copyWidth, (uint)canvasHeight);
                    using var croppedFrame = frame.Clone();
                    croppedFrame.Crop(cropGeometry);
                    croppedFrame.ResetPage();
                    newImage.Composite(croppedFrame, 0, 0, CompositeOperator.Over);
                    newImage.AnimationDelay = (uint)recalculatedDelays[frameIndex];
                    newImage.AnimationTicksPerSecond = ticksPerSecond;
                    newImage.GifDisposeMethod = GifDisposeMethod.Background;
                    partCollection.Add(newImage.Clone());
                }

                partCollection.Optimize();
                partCollection[0].AnimationTicksPerSecond = ticksPerSecond;
                foreach (var frame in partCollection)
                {
                    frame.Settings.SetDefine("compress", "LZW");
                    frame.Settings.SetDefine(MagickFormat.Gif, "optimize-transparency", "true");
                }

                string outputFile = Path.Combine(outputDirectory, $"{Path.GetFileNameWithoutExtension(inputFilePath)}_Part{i + 1}.gif");
                partCollection.Write(outputFile);
                ModifyGifFile(outputFile, canvasHeight);
            }
        }

        public static async Task SplitGifWithReducedPalette(GifToolMainForm mainForm)
        {
            // Keep the original method name for backward compatibility
            // but redirect to the new merge and split functionality
            await MergeAndSplitFiveGifs(mainForm);
        }

        [Obsolete("This method has been replaced with MergeAndSplitFiveGifs")]
        public static async Task SplitGifWithReducedPaletteOld(GifToolMainForm mainForm)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = SteamGifCropper.Properties.Resources.FileDialog_GifFilter,
                Title = SteamGifCropper.Properties.Resources.FileDialog_SelectGif
            };

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                string inputFilePath = openFileDialog.FileName;
                ImageInputValidator.ValidateGif(inputFilePath);

                try
                {
                    using (var collection = new MagickImageCollection(inputFilePath))
                    {
                        uint canvasWidth = collection[0].Page.Width;
                        uint canvasHeight = collection[0].Page.Height;

                        if (!IsValidCanvasWidth(canvasWidth))
                        {
                            ShowUnsupportedWidthError(canvasWidth);
                            return;
                        }

                        int paletteSize = (int)mainForm.numUpDownPalette.Value; // Get palette size from numericUpDown
                        if (paletteSize < 32 || paletteSize > 256)
                        {
                            WindowsThemeManager.ShowThemeAwareMessageBox(mainForm,
                                            SteamGifCropper.Properties.Resources.Error_PaletteRange,
                                            SteamGifCropper.Properties.Resources.Title_Error,
                                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return;
                        }

                        SetStatusText(mainForm, SteamGifCropper.Properties.Resources.Status_ProcessingPalette);
                        mainForm.pBarTaskStatus.Minimum = 0;
                        mainForm.pBarTaskStatus.Maximum = 100;
                        SetProgressBar(mainForm.pBarTaskStatus, 0, mainForm.pBarTaskStatus.Maximum);

                        var ranges = GetCropRanges(canvasWidth);

                        var progress = new Progress<(int current, int total, string status)>(report =>
                        {
                            mainForm.Invoke((MethodInvoker)(() =>
                            {
                                if (report.total > 0)
                                {
                                    SetProgressBar(mainForm.pBarTaskStatus, Math.Min(report.current * 100 / report.total, 100), mainForm.pBarTaskStatus.Maximum);
                                }
                                SetStatusText(mainForm, report.status);
                            }));
                        });

                        await ReducePaletteAndSplitGif(inputFilePath, ranges, (int)canvasHeight, paletteSize, progress);
                        SetStatusText(mainForm, SteamGifCropper.Properties.Resources.Status_Done);
                        WindowsThemeManager.ShowThemeAwareMessageBox(mainForm,
                                        SteamGifCropper.Properties.Resources.Message_PaletteProcessingComplete,
                                        SteamGifCropper.Properties.Resources.Title_Success, MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
                catch (Exception ex)
                {
                    SetStatusText(mainForm, SteamGifCropper.Properties.Resources.Status_Error);
                    WindowsThemeManager.ShowThemeAwareMessageBox(mainForm,
                                    string.Format(SteamGifCropper.Properties.Resources.Error_Occurred, ex.Message),
                                    SteamGifCropper.Properties.Resources.Title_Error, MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                finally
                {
                    SetProgressBar(mainForm.pBarTaskStatus, 0, mainForm.pBarTaskStatus.Maximum);
                    SetStatusText(mainForm, SteamGifCropper.Properties.Resources.Status_Idle);
                }
            }
        }

        public static void DiagnoseGif(string gifPath)
        {
            using var collection = new MagickImageCollection(gifPath);
            Console.WriteLine($"Total frames: {collection.Count}");
            Console.WriteLine($"AnimationTicksPerSecond: {collection[0].AnimationTicksPerSecond}");
            Console.WriteLine($"AnimationIterations: {collection[0].AnimationIterations}");
            Console.WriteLine("Frame delays:");
            for (int i = 0; i < Math.Min(collection.Count, 10); i++)
            {
                Console.WriteLine($"Frame {i}: {collection[i].AnimationDelay} cs");
            }
            double totalDelay = collection.Sum(frame => (double)frame.AnimationDelay);
            double averageFps = collection.Count * 100.0 / totalDelay;
            Console.WriteLine($"Average FPS: {averageFps:F2}");
        }

        private static async Task ReducePaletteAndSplitGif(string inputFilePath, (int Start, int End)[] ranges, int canvasHeight, int paletteSize, IProgress<(int current, int total, string status)> progress)
        {
            await Task.Run(() =>
            {
                using (var collection = new MagickImageCollection(inputFilePath))
                {
                    progress?.Report((0, 1, SteamGifCropper.Properties.Resources.Status_CoalescingFrames));
                    collection.Coalesce();

                    int newHeight = canvasHeight + HeightExtension;

                    // Preserve original frame delays
                    var originalDelays = collection.Select(f => (int)f.AnimationDelay).ToArray();

                    int totalSteps = (collection.Count * ranges.Length) + (ranges.Length * 3); // Processing + Palette reduction + LZW compression
                    int currentStep = 0;

                    for (int i = 0; i < ranges.Length; i++)
                    {
                        using (var partCollection = new MagickImageCollection())
                        {
                            for (int frameIndex = 0; frameIndex < collection.Count; frameIndex++)
                            {
                                var frame = collection[frameIndex];
                                int copyWidth = ranges[i].End - ranges[i].Start + 1;

                                // Create new image with correct dimensions
                                using (var newImage = new MagickImage(MagickColors.Transparent, (uint)copyWidth, (uint)newHeight))
                                {
                                    // Crop the frame to the specific range
                                    var cropGeometry = new MagickGeometry(ranges[i].Start, 0, (uint)copyWidth, (uint)canvasHeight);
                                    using (var croppedFrame = frame.Clone())
                                    {
                                        croppedFrame.Crop(cropGeometry);
                                        croppedFrame.ResetPage();

                                        // Composite the cropped frame onto the new image
                                        newImage.Composite(croppedFrame, 0, 0, CompositeOperator.Over);
                                    }

                                    // Set animation properties to preserve original timing
                                    newImage.AnimationDelay = (uint)originalDelays[frameIndex];
                                    newImage.GifDisposeMethod = GifDisposeMethod.Background;

                                    // Apply palette reduction
                                    newImage.Quantize(new QuantizeSettings { Colors = (uint)paletteSize });

                                    partCollection.Add(newImage.Clone());
                                }

                                currentStep++;
                                progress?.Report((currentStep, totalSteps, string.Format(SteamGifCropper.Properties.Resources.Status_ProcessingPartPalette, i + 1, currentStep % collection.Count + 1)));
                            }

                            string outputFile = $"{Path.GetFileNameWithoutExtension(inputFilePath)}_Part{i + 1}_Palette{paletteSize}.gif";
                            string outputDir = Path.GetDirectoryName(inputFilePath);
                            string outputPath = Path.Combine(outputDir, outputFile);

                            partCollection.Optimize();
                            partCollection[0].AnimationTicksPerSecond = 100;
                            currentStep++;
                            progress?.Report((currentStep, totalSteps, SteamGifCropper.Properties.Resources.Status_Compressing));

                            foreach (var frame in partCollection)
                            {
                                frame.Settings.SetDefine("compress", "LZW");
                                frame.Settings.SetDefine(MagickFormat.Gif, "optimize-transparency", "true");
                            }

                            currentStep++;
                            progress?.Report((currentStep, totalSteps, SteamGifCropper.Properties.Resources.Status_Saving));

                            partCollection.Write(outputPath);

                            currentStep++;
                            progress?.Report((currentStep, totalSteps, SteamGifCropper.Properties.Resources.Status_Saving));

                            ModifyGifFile(outputPath, canvasHeight);
                        }
                    }
                }
            });
        }
        private static void ModifyGifFile(string filePath, int adjustedHeight)
        {
            try
            {
                byte[] fileData = File.ReadAllBytes(filePath);
                if (fileData.Length < 10)
                {
                    throw new InvalidOperationException($"Invalid GIF file: {filePath}");
                }

                // Modify tail byte from 0x3B to 0x21
                fileData[fileData.Length - 1] = 0x21;

                // Update height bytes
                ushort heightValue = (ushort)adjustedHeight;
                fileData[8] = (byte)(heightValue & 0xFF);
                fileData[9] = (byte)((heightValue >> 8) & 0xFF);

                File.WriteAllBytes(filePath, fileData);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to modify GIF file {filePath}: {ex.Message}", ex);
            }
        }
        // Accepts a GIF or a still image (PNG/JPEG/BMP/WebP/HEIC/...). A still image loads as a
        // single-frame collection, so it resizes and writes out as a single-frame 766px GIF.
        public static void ResizeGifTo766(string inputFilePath, string outputFilePath, GifToolMainForm mainForm = null)
        {
            ImageInputValidator.ValidateImage(inputFilePath);
            try
            {
                SetStatusText(mainForm, SteamGifCropper.Properties.Resources.Status_CoalescingFrames);
                using (var collection = new MagickImageCollection(inputFilePath))
                {
                    collection.Coalesce();

                    int totalFrames = collection.Count;
                    int currentFrame = 0;

                    if (mainForm != null)
                    {
                        SetProgressRange(mainForm, 0, totalFrames);
                        SetProgressBar(mainForm.pBarTaskStatus, 0, totalFrames);
                        UpdateFrameProgressByFrame(mainForm, 0, totalFrames);
                    }

                    foreach (var frame in collection)
                    {
                        frame.ResetPage();
                        frame.Resize(SupportedWidth1, 0);
                        frame.Settings.SetDefine("compress", "LZW");

                        currentFrame++;
                        if (mainForm != null)
                        {
                            UpdateFrameProgressByFrame(mainForm, currentFrame, totalFrames);
                        }
                    }

                    // Optimize + write are each a long blocking call; OptimizeAndWriteWithProgress surfaces
                    // the phase label and animates the bar from ImageMagick's per-frame progress.
                    OptimizeAndWriteWithProgress(mainForm, collection, outputFilePath);
                }
            }
            finally
            {
                if (mainForm != null)
                {
                    SetProgressBar(mainForm.pBarTaskStatus, 0, 100);
                    SetStatusText(mainForm, SteamGifCropper.Properties.Resources.Status_Idle);
                }
            }
        }

        public static async Task ResizeGifTo766(GifToolMainForm mainForm)
        {
            using (var openFileDialog = new OpenFileDialog
            {
                Filter = SteamGifCropper.Properties.Resources.FileDialog_ImageAndGifFilter,
                Title = SteamGifCropper.Properties.Resources.FileDialog_SelectGifResize
            })
            {
                if (openFileDialog.ShowDialog() != DialogResult.OK) return;

                string inputFilePath = openFileDialog.FileName;
                ImageInputValidator.ValidateImage(inputFilePath);
                // Always emit a 766px GIF (single-frame for a still image) so the result feeds the
                // GIF-only tools (grid mosaic, split, ...) and the 766px pipeline regardless of input.
                string outputFilePath = Path.Combine(
                    Path.GetDirectoryName(inputFilePath),
                    Path.GetFileNameWithoutExtension(inputFilePath) + "_766px.gif");

                mainForm.Enabled = false;
                try
                {
                    SetProgressRange(mainForm, 0, 100);
                    SetProgressBar(mainForm.pBarTaskStatus, 0, 100);
                    SetProgressVisible(mainForm, true);
                    SetStatusText(mainForm, SteamGifCropper.Properties.Resources.Status_Loading);

                    await Task.Run(() => ResizeGifTo766(inputFilePath, outputFilePath, mainForm));

                    WindowsThemeManager.ShowThemeAwareMessageBox(mainForm,
                                    string.Format(SteamGifCropper.Properties.Resources.Message_ResizeComplete,
                                                  Path.GetFileName(outputFilePath)),
                                    SteamGifCropper.Properties.Resources.Title_Success, MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    WindowsThemeManager.ShowThemeAwareMessageBox(mainForm,
                                    string.Format(SteamGifCropper.Properties.Resources.Error_ResizeFailed, ex.Message),
                                    SteamGifCropper.Properties.Resources.Title_Error, MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                finally
                {
                    mainForm.Enabled = true;
                    SetProgressBar(mainForm.pBarTaskStatus, 0, 100);
                    SetStatusText(mainForm, SteamGifCropper.Properties.Resources.Status_Idle);
                }
            }
        }

    }
}
