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
        public static async Task StartProcessing(GifToolMainForm mainForm)
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
                SetStatusText(mainForm, "Split GIF...");
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

                        var gifsicle = CaptureGifsicleSnapshot(mainForm);
                        mainForm.Enabled = false;
                        SetProgressRange(mainForm, 0, 100);
                        SetProgressBar(mainForm.pBarTaskStatus, 0, 100);
                        SetStatusText(mainForm, SteamGifCropper.Properties.Resources.Status_Processing);

                        var ranges = GetCropRanges(canvasWidth);

                        await SplitGif(inputFilePath, mainForm, ranges, (int)canvasHeight, gifsicle);
                        SetStatusText(mainForm, SteamGifCropper.Properties.Resources.Status_Done);
                        WindowsThemeManager.ShowThemeAwareMessageBox(mainForm,
                                        SteamGifCropper.Properties.Resources.Message_ProcessingComplete,
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
                    mainForm.Enabled = true;
                    SetProgressBar(mainForm.pBarTaskStatus, 0, mainForm.pBarTaskStatus.Maximum);
                    SetStatusText(mainForm, SteamGifCropper.Properties.Resources.Status_Idle);
                }
            }
        }

        // Draws a grid/lattice aligned to the Steam showcase slots over a 766/774 GIF, turning the
        // forced 4px/6px slot gaps into a deliberate mosaic. Outputs a single full-width GIF (no split)
        // so it can be chained; split later with the main "Split GIF" button.
        public static async Task GridMosaic(GifToolMainForm mainForm)
        {
            using (var dialog = new GridMosaicDialog())
            {
                if (dialog.ShowDialog() != DialogResult.OK)
                {
                    return;
                }

                string inputFilePath = dialog.InputFilePath;
                ImageInputValidator.ValidateGif(inputFilePath);

                uint canvasWidth;
                using (var probe = new MagickImageCollection(inputFilePath))
                {
                    canvasWidth = probe[0].Page.Width;
                }
                if (!IsValidCanvasWidth(canvasWidth))
                {
                    ShowUnsupportedWidthError(canvasWidth);
                    return;
                }

                var grid = new GridMosaicSettings
                {
                    InputFilePath = inputFilePath,
                    ColumnsPerSlot = dialog.ColumnsPerSlot,
                    Rows = dialog.Rows,
                    LineWidth = dialog.LineWidth,
                    Style = dialog.Style,
                    LineColor = dialog.LineColor
                };
                string outputFilePath = GenerateOutputPath(inputFilePath, "_grid");

                mainForm.Enabled = false;
                SetProgressRange(mainForm, 0, 100);
                SetProgressBar(mainForm.pBarTaskStatus, 0, 100);
                SetStatusText(mainForm, SteamGifCropper.Properties.Resources.Status_Processing);
                try
                {
                    await ApplyGridMosaic(mainForm, inputFilePath, outputFilePath, grid);

                    SetProgressBar(mainForm.pBarTaskStatus, 100, 100);
                    SetStatusText(mainForm, SteamGifCropper.Properties.Resources.Status_Done);
                    WindowsThemeManager.ShowThemeAwareMessageBox(mainForm,
                                    SteamGifCropper.Properties.Resources.Message_ProcessingComplete,
                                    SteamGifCropper.Properties.Resources.Title_Success, MessageBoxButtons.OK, MessageBoxIcon.Information);
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
                    mainForm.Enabled = true;
                    SetProgressBar(mainForm.pBarTaskStatus, 0, 100);
                    SetStatusText(mainForm, SteamGifCropper.Properties.Resources.Status_Idle);
                }
            }
        }

        // Overlays the slot-aligned grid onto every frame of the full-width GIF (no split). The grid is
        // built once per slot and composited at each slot's x-offset, so the result matches the old
        // per-part appearance (gaps stay grid-free). Runs on a background thread.
        private static async Task ApplyGridMosaic(GifToolMainForm mainForm, string inputFilePath,
            string outputFilePath, GridMosaicSettings grid)
        {
            await Task.Run(() =>
            {
                using var collection = LoadCoalesceWithProgress(mainForm, inputFilePath);

                uint canvasWidth = collection[0].Width;
                int canvasHeight = (int)collection[0].Height;
                var ranges = GetCropRanges(canvasWidth);

                var gridLayers = new MagickImage[ranges.Length];
                try
                {
                    for (int c = 0; c < ranges.Length; c++)
                    {
                        int partWidth = ranges[c].End - ranges[c].Start + 1;
                        gridLayers[c] = GridMosaicRenderer.BuildGridLayer((uint)partWidth, (uint)canvasHeight, canvasHeight, grid);
                    }

                    var op = grid.Style == GridLineStyle.Transparent ? CompositeOperator.DstOut : CompositeOperator.Over;
                    int total = collection.Count;
                    int idx = 0;
                    foreach (var frame in collection)
                    {
                        for (int c = 0; c < ranges.Length; c++)
                        {
                            frame.Composite(gridLayers[c], ranges[c].Start, 0, op);
                        }
                        frame.Settings.SetDefine("compress", "LZW");

                        idx++;
                        if (idx % 10 == 0 || idx == total)
                        {
                            SetProgressBar(mainForm.pBarTaskStatus, idx * 90 / total, 100);
                            SetStatusText(mainForm, SteamGifCropper.Properties.Resources.Status_Processing);
                        }
                    }

                    OptimizeAndWriteWithProgress(mainForm, collection, outputFilePath);
                }
                finally
                {
                    foreach (var layer in gridLayers)
                    {
                        layer?.Dispose();
                    }
                }
            });
        }


        private static (int[] delays, int ticksPerSecond) RecalculateGifDelays(MagickImageCollection collection)
        {
            int sourceTicks = (int)collection[0].AnimationTicksPerSecond;
            if (sourceTicks <= 0)
            {
                sourceTicks = 100;
            }

            // Always preserve original frame delays and timing
            var originalDelays = collection.Select(f => (int)f.AnimationDelay).ToArray();
            return (originalDelays, sourceTicks);
        }

        private static async Task SplitGif(string inputFilePath, GifToolMainForm mainForm, (int Start, int End)[] ranges, int canvasHeight, GifsicleSnapshot gifsicle)
        {
            // All heavy ImageMagick work runs on a background thread so the UI stays responsive.
            // UI updates go exclusively through the marshaled SetProgressBar/SetStatusText/SetProgress*
            // helpers; no direct control access happens inside this lambda.
            await Task.Run(async () =>
            {
                using var collection = LoadCoalesceWithProgress(mainForm, inputFilePath);
                int newHeight = canvasHeight + HeightExtension;

                var (recalculatedDelays, ticksPerSecond) = RecalculateGifDelays(collection);
                collection[0].AnimationTicksPerSecond = ticksPerSecond;

                int totalFrames = collection.Count * ranges.Length;
                int currentFrame = 0;

                for (int i = 0; i < ranges.Length; i++)
                {
                    using (var partCollection = new MagickImageCollection())
                    {
                        for (int frameIndex = 0; frameIndex < collection.Count; frameIndex++)
                        {
                            var frame = collection[frameIndex];
                            int copyWidth = ranges[i].End - ranges[i].Start + 1;

                            if (currentFrame % ProgressUpdateInterval == 0)
                            {
                                SetStatusText(mainForm, string.Format("Splitting part {0}/5 - Frame {1}/{2}", i + 1, (currentFrame % collection.Count) + 1, collection.Count));
                            }

                            MagickImage newImage = null;
                            try
                            {
                                newImage = new MagickImage(MagickColors.Transparent, (uint)copyWidth, (uint)newHeight);

                                var cropGeometry = new MagickGeometry(ranges[i].Start, 0, (uint)copyWidth, (uint)canvasHeight);
                                using (var croppedFrame = frame.Clone())
                                {
                                    croppedFrame.Crop(cropGeometry);
                                    croppedFrame.ResetPage();
                                    newImage.Composite(croppedFrame, 0, 0, CompositeOperator.Over);
                                }

                                newImage.AnimationDelay = (uint)recalculatedDelays[frameIndex];
                                newImage.AnimationTicksPerSecond = ticksPerSecond;
                                newImage.GifDisposeMethod = GifDisposeMethod.Background;

                                partCollection.Add(newImage);
                                newImage = null; // Ownership transferred to collection
                            }
                            finally
                            {
                                // Dispose only if not added to collection
                                newImage?.Dispose();
                            }

                            currentFrame++;
                            UpdateFrameProgress(mainForm, currentFrame, totalFrames);
                        }

                        string outputFile = $"{Path.GetFileNameWithoutExtension(inputFilePath)}_Part{i + 1}.gif";
                        string outputDir = Path.GetDirectoryName(inputFilePath);
                        string outputPath = Path.Combine(outputDir, outputFile);

                        RunMagickWithProgress(mainForm, partCollection,
                            SteamGifCropper.Properties.Resources.Status_Optimizing, () => partCollection.Optimize());
                        partCollection[0].AnimationTicksPerSecond = ticksPerSecond;
                        SetStatusText(mainForm, SteamGifCropper.Properties.Resources.Status_Compressing);
                        int compressFrameCount = 0;
                        foreach (var frame in partCollection)
                        {
                            frame.Settings.SetDefine("compress", "LZW");
                            frame.Settings.SetDefine(MagickFormat.Gif, "optimize-transparency", "true");

                            if (++compressFrameCount % 25 == 0)
                            {
                                SetStatusText(mainForm, SteamGifCropper.Properties.Resources.Status_Compressing);
                            }
                        }

                        SetProgressVisible(mainForm, true);

                        RunMagickWithProgress(mainForm, partCollection,
                            SteamGifCropper.Properties.Resources.Status_Saving, () => partCollection.Write(outputPath));

                        // Keep the overall progress monotonic: each part owns one band of the bar
                        // (e.g. 0-20-40-60-80-100 for 5 parts) instead of spiking to 100% per part,
                        // which made the themed progress bar sweep to the far right on every part.
                        int partSpan = 100 / ranges.Length;
                        int partBase = i * partSpan;

                        var progress = new Progress<int>(p =>
                        {
                            SetProgressBar(mainForm.pBarTaskStatus, partBase + p * partSpan / 100, 100);
                            SetStatusText(mainForm, $"{SteamGifCropper.Properties.Resources.Status_GifsicleOptimizing} ({p}%)");
                        });

                        if (gifsicle.AutoFit)
                        {
                            await AutoFitFileInPlace(mainForm, gifsicle, outputPath, progress);
                        }
                        else
                        {
                            await OptimizeWithGifsicleIfEnabled(mainForm, gifsicle, outputPath, progress);
                        }

                        // Finalize this part's band regardless of whether gifsicle ran, was skipped by
                        // the size threshold, or was disabled — keeps the bar monotonic.
                        SetProgressBar(mainForm.pBarTaskStatus, partBase + partSpan, 100);
                        SetStatusText(mainForm, $"Saving part {i + 1} complete");

                        ModifyGifFile(outputPath, canvasHeight);
                    }
                }
            });
        }

    }
}
