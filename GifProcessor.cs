using System;
using System.IO;
using System.Windows.Forms;
using ImageMagick;

namespace GifProcessorApp
{
    public static class GifProcessor
    {
        private static readonly (int Start, int End)[] Ranges766 = { (0, 149), (154, 303), (308, 457), (462, 611), (616, 765) };
        private static readonly (int Start, int End)[] Ranges774 = { (0, 149), (155, 305), (311, 461), (467, 617), (623, 773) };
        private const int HeightExtension = 100;
        private const uint SupportedWidth1 = 766;
        private const uint SupportedWidth2 = 774;

        private static bool IsValidCanvasWidth(uint width) => width == SupportedWidth1 || width == SupportedWidth2;

        private static void ShowUnsupportedWidthError(uint width)
        {
            string message = $"Unsupported GIF canvas width: {width}px. Only {SupportedWidth1}px and {SupportedWidth2}px are supported.";
            MessageBox.Show(message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private static (int Start, int End)[] GetCropRanges(uint canvasWidth)
        {
            return canvasWidth == SupportedWidth1 ? Ranges766 : Ranges774;
        }

        private static void UpdateProgress(ProgressBar progressBar, int current, int total)
        {
            if (progressBar != null && total > 0)
            {
                progressBar.Value = Math.Min((int)((double)current / total * 100), 100);
                Application.DoEvents();
            }
        }

        public static void StartProcessing(GifToolMainForm mainForm)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "GIF Files (*.gif)|*.gif",
                Title = "Select a GIF file to process"
            };

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                string inputFilePath = openFileDialog.FileName;

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

                        mainForm.lblStatus.Text = "Processing...";
                        mainForm.pBarTaskStatus.Minimum = 0;
                        mainForm.pBarTaskStatus.Maximum = 100;
                        mainForm.pBarTaskStatus.Value = 0;
                        Application.DoEvents();

                        var ranges = GetCropRanges(canvasWidth);
                        SplitGif(collection, inputFilePath, mainForm, ranges, (int)canvasHeight);
                        mainForm.lblStatus.Text = "Done.";
                        MessageBox.Show("GIF processing and splitting completed successfully!",
                                        "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);

                    }
                }
                catch (Exception ex)
                {
                    mainForm.lblStatus.Text = "Error.";
                    MessageBox.Show($"An error occurred: {ex.Message}",
                                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                finally
                {
                    mainForm.pBarTaskStatus.Value = 0;
                    mainForm.lblStatus.Text = "Idle.";
                }
            }
        }
        private static void SplitGif(MagickImageCollection collection, string inputFilePath, GifToolMainForm mainForm, (int Start, int End)[] ranges, int canvasHeight)
        {
            mainForm.lblStatus.Text = "Coalescing frames...";
            Application.DoEvents();
            collection.Coalesce();
            Application.DoEvents();
            int newHeight = canvasHeight + HeightExtension;

            int totalSteps = (collection.Count * ranges.Length) + (ranges.Length * 2);
            int currentStep = 0;

                for (int i = 0; i < ranges.Length; i++)
                {
                    using (var partCollection = new MagickImageCollection())
                    {
                        foreach (var frame in collection)
                        {
                            uint originalDelay = frame.AnimationDelay;
                            int copyWidth = ranges[i].End - ranges[i].Start + 1;
                            frame.ResetPage();
                            frame.Extent(new MagickGeometry((uint)(ranges[i].End + 1), (uint)canvasHeight), Gravity.Northwest);

                            mainForm.lblStatus.Text = "Split...";
                            Application.DoEvents();
                            
                            using (var newImage = new MagickImage(MagickColors.Transparent, (uint)copyWidth, (uint)newHeight))
                            {
                                int cropStartX = Math.Max(ranges[i].Start, 0);
                                newImage.Composite(frame, -cropStartX, 0, CompositeOperator.Copy);
                                newImage.Extent(new MagickGeometry((uint)copyWidth, (uint)newHeight), Gravity.North);
                                newImage.AnimationDelay = originalDelay;
                                
                                partCollection.Add(newImage.Clone());
                            }

                            mainForm.lblStatus.Text = "Add frame...";
                            currentStep++;
                            UpdateProgress(mainForm.pBarTaskStatus, currentStep, totalSteps);
                        }

                        string outputFile = $"{Path.GetFileNameWithoutExtension(inputFilePath)}_Part{i + 1}.gif";
                        string outputDir = Path.GetDirectoryName(inputFilePath);
                        string outputPath = Path.Combine(outputDir, outputFile);

                        partCollection.Optimize();
                        mainForm.lblStatus.Text = "Compressing...";
                        Application.DoEvents();
                        foreach (var frame in partCollection)
                        {
                            frame.Settings.SetDefine("compress", "LZW");
                        }

                        currentStep++;
                        UpdateProgress(mainForm.pBarTaskStatus, currentStep, totalSteps);
                        mainForm.lblStatus.Text = "Saving...";
                        Application.DoEvents();

                        partCollection.Write(outputPath);

                        if (mainForm.chkGifsicle.Checked)
                        {
                            mainForm.lblStatus.Text = "gifsicle-ing...";
                            Application.DoEvents();
                            var options = new GifsicleWrapper.GifsicleOptions
                            {
                                Colors = (int)mainForm.numUpDownPaletteSicle.Value,
                                Lossy = (int)mainForm.numUpDownLossy.Value,
                                OptimizeLevel = (int)mainForm.numUpDownOptimize.Value,
                                Dither = mainForm.DitherMethod
                            };

                            GifsicleWrapper.OptimizeGif(outputPath, outputPath, options);
                        }

                        currentStep++;
                        UpdateProgress(mainForm.pBarTaskStatus, currentStep, totalSteps);
                        Application.DoEvents();

                        ModifyGifFile(outputPath, canvasHeight);
                  }
              }
          }

        public static void SplitGifWithReducedPalette(GifToolMainForm mainForm)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "GIF Files (*.gif)|*.gif",
                Title = "Select a GIF file to process"
            };

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                string inputFilePath = openFileDialog.FileName;

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
                            MessageBox.Show("Palette size must be between 32 and 256.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return;
                        }

                        mainForm.lblStatus.Text = "Processing with reduced palette...";
                        mainForm.pBarTaskStatus.Minimum = 0;
                        mainForm.pBarTaskStatus.Maximum = 100;
                        mainForm.pBarTaskStatus.Value = 0;
                        Application.DoEvents();

                        var ranges = GetCropRanges(canvasWidth);
                        ReducePaletteAndSplitGif(inputFilePath, mainForm, ranges, (int)canvasHeight, paletteSize);
                        mainForm.lblStatus.Text = "Done.";
                        MessageBox.Show("GIF processing with reduced palette completed successfully!",
                                        "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
                catch (Exception ex)
                {
                    mainForm.lblStatus.Text = "Error.";
                    MessageBox.Show($"An error occurred: {ex.Message}",
                                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                finally
                {
                    mainForm.pBarTaskStatus.Value = 0;
                    mainForm.lblStatus.Text = "Idle.";
                }
            }
        }

        private static void ReducePaletteAndSplitGif(string inputFilePath, GifToolMainForm mainForm, (int Start, int End)[] ranges, int canvasHeight, int paletteSize)
        {
            using (var collection = new MagickImageCollection(inputFilePath))
            {
                mainForm.lblStatus.Text = "Coalescing frames...";
                Application.DoEvents();
                collection.Coalesce();
                Application.DoEvents();

                int newHeight = canvasHeight + HeightExtension;

                int totalSteps = (collection.Count * ranges.Length) + (ranges.Length * 3); // Processing + Palette reduction + LZW compression
                int currentStep = 0;

                for (int i = 0; i < ranges.Length; i++)
                {
                    using (var partCollection = new MagickImageCollection())
                    {
                        foreach (var frame in collection)
                        {
                            uint originalDelay = frame.AnimationDelay;
                            int copyWidth = ranges[i].End - ranges[i].Start + 1;

                            frame.ResetPage();
                            frame.Extent(new MagickGeometry((uint)(ranges[i].End + 1), (uint)canvasHeight), Gravity.Northwest);

                            mainForm.lblStatus.Text = "Splitting...";
                            Application.DoEvents();
                            
                            using (var newImage = new MagickImage(MagickColors.Transparent, (uint)copyWidth, (uint)newHeight))
                            {
                                int cropStartX = Math.Max(ranges[i].Start, 0);
                                newImage.Composite(frame, -cropStartX, 0, CompositeOperator.Copy);
                                newImage.Extent(new MagickGeometry((uint)copyWidth, (uint)newHeight), Gravity.North);
                                newImage.AnimationDelay = originalDelay;

                                mainForm.lblStatus.Text = "Reducing palette...";
                                Application.DoEvents();
                                newImage.Quantize(new QuantizeSettings { Colors = (uint)paletteSize });

                                partCollection.Add(newImage.Clone());
                            }

                            currentStep++;
                            UpdateProgress(mainForm.pBarTaskStatus, currentStep, totalSteps);
                        }

                        string outputFile = $"{Path.GetFileNameWithoutExtension(inputFilePath)}_Part{i + 1}_Palette{paletteSize}.gif";
                        string outputDir = Path.GetDirectoryName(inputFilePath);
                        string outputPath = Path.Combine(outputDir, outputFile);

                        partCollection.Optimize();
                        mainForm.lblStatus.Text = "Compressing...";
                        Application.DoEvents();
                        foreach (var frame in partCollection)
                        {
                            frame.Settings.SetDefine("compress", "LZW");
                        }

                        currentStep++;
                        UpdateProgress(mainForm.pBarTaskStatus, currentStep, totalSteps);
                        mainForm.lblStatus.Text = "Saving...";
                        Application.DoEvents();

                        partCollection.Write(outputPath);

                        currentStep++;
                        UpdateProgress(mainForm.pBarTaskStatus, currentStep, totalSteps);
                        Application.DoEvents();

                        ModifyGifFile(outputPath, canvasHeight);
                    }
                }
            }
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
        public static void ResizeGifTo766(GifToolMainForm mainForm)
        {
            using (var openFileDialog = new OpenFileDialog
            {
                Filter = "GIF Files (*.gif)|*.gif",
                Title = "Select a GIF file to resize"
            })
            {
                if (openFileDialog.ShowDialog() != DialogResult.OK) return;

                string inputFilePath = openFileDialog.FileName;
                string outputFilePath = GenerateOutputPath(inputFilePath, "_766px");

                try
                {
                    mainForm.lblStatus.Text = "Loading...";
                    mainForm.pBarTaskStatus.Value = 0;
                    mainForm.pBarTaskStatus.Maximum = 100;
                    mainForm.pBarTaskStatus.Visible = true;
                    Application.DoEvents();

                    using (var collection = new MagickImageCollection(inputFilePath))
                    {
                        collection.Coalesce();
                        
                        int totalSteps = collection.Count * 2;
                        int currentStep = 0;

                        // Resize frames
                        mainForm.lblStatus.Text = "Resizing frames...";
                        foreach (var frame in collection)
                        {
                            frame.ResetPage();
                            frame.Resize(SupportedWidth1, 0);
                            frame.Settings.SetDefine("compress", "LZW");
                            
                            currentStep++;
                            UpdateProgress(mainForm.pBarTaskStatus, currentStep, totalSteps);
                        }

                        // Optimize and save
                        mainForm.lblStatus.Text = "Optimizing...";
                        collection.Optimize();
                        
                        currentStep = totalSteps;
                        UpdateProgress(mainForm.pBarTaskStatus, currentStep, totalSteps);
                        
                        mainForm.lblStatus.Text = "Saving...";
                        collection.Write(outputFilePath);

                        MessageBox.Show(mainForm, $"GIF resizing completed successfully!\nSaved as: {Path.GetFileName(outputFilePath)}",
                                        "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(mainForm, $"Resize operation failed: {ex.Message}",
                                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                finally
                {
                    mainForm.pBarTaskStatus.Value = 0;
                    mainForm.lblStatus.Text = "Idle.";
                }
            }
        }

        private static string GenerateOutputPath(string inputPath, string suffix)
        {
            string directory = Path.GetDirectoryName(inputPath);
            string fileName = Path.GetFileNameWithoutExtension(inputPath);
            string extension = Path.GetExtension(inputPath);
            return Path.Combine(directory, $"{fileName}{suffix}{extension}");
        }
        public static void WriteTailByteForMultipleGifs(GifToolMainForm mainForm)
        {
            using (var openFileDialog = new OpenFileDialog
            {
                Filter = "GIF Files (*.gif)|*.gif",
                Title = "Select GIF files to process",
                Multiselect = true
            })
            {
                if (openFileDialog.ShowDialog() != DialogResult.OK) return;

                string[] filePaths = openFileDialog.FileNames;
                int processedFiles = 0;

                try
                {
                    mainForm.pBarTaskStatus.Visible = true;
                    mainForm.pBarTaskStatus.Value = 0;
                    mainForm.pBarTaskStatus.Maximum = filePaths.Length;

                    foreach (string filePath in filePaths)
                    {
                        try
                        {
                            if (ProcessTailByte(filePath))
                                processedFiles++;

                            UpdateProgress(mainForm.pBarTaskStatus, processedFiles, filePaths.Length);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show(mainForm, $"Error processing file {Path.GetFileName(filePath)}:\n{ex.Message}",
                                            "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }
                    }

                    MessageBox.Show(mainForm, $"{processedFiles} of {filePaths.Length} GIF files processed successfully!",
                                    "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(mainForm, $"An error occurred: {ex.Message}",
                                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                finally
                {
                    mainForm.pBarTaskStatus.Value = 0;
                    mainForm.pBarTaskStatus.Visible = false;
                    mainForm.lblStatus.Text = "Idle.";
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

    }
}
