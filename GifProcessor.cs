using System;
using System.CodeDom;
using System.IO;
using System.Linq.Expressions;
using System.Windows.Forms;
using ImageMagick;

namespace GifProcessorApp
{
    public static class GifProcessor
    {
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

                        if (canvasWidth != 766 && canvasWidth != 774)
                        {
                            string message = $"Unsupported GIF canvas width: {canvasWidth}px. Only 766px and 774px are supported.";
                            MessageBox.Show(message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return;
                        }

                        mainForm.lblStatus.Text = "Processing...";
                        mainForm.pBarTaskStatus.Minimum = 0;
                        mainForm.pBarTaskStatus.Maximum = 100;
                        mainForm.pBarTaskStatus.Value = 0;
                        Application.DoEvents();

                        SplitGif(inputFilePath, mainForm.pBarTaskStatus, (int)canvasWidth, (int)canvasHeight, mainForm.lblStatus);
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
        private static void SplitGif(string inputFilePath, ProgressBar progressBar, int canvasWidth, int canvasHeight, Label labelSt)
        {
            using (var collection = new MagickImageCollection(inputFilePath))
            {
                labelSt.Text = "Processing.....";
                Application.DoEvents();
                collection.Coalesce();
                Application.DoEvents();
                int newHeight = canvasHeight + 100;

                (int Start, int End)[] ranges = canvasWidth == 766
                    ? new (int Start, int End)[] { (0, 149), (154, 303), (308, 457), (462, 611), (616, canvasWidth - 1) }
                    : new (int Start, int End)[] { (0, 149), (155, 305), (311, 461), (467, 617), (623, canvasWidth - 1) };

                int totalSteps = (collection.Count * ranges.Length) + (ranges.Length * 2); // Processing + LZW compression + Writing
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
                            frame.Extent(new MagickGeometry((uint)canvasWidth, (uint)canvasHeight), Gravity.Northwest);

                            labelSt.Text = "Split...";
                            Application.DoEvents();
                            MagickImage newImage = new MagickImage(MagickColors.Transparent, (uint)copyWidth, (uint)newHeight);
                            int cropStartX = ranges[i].Start;
                            if (cropStartX < 0) cropStartX = 0;

                            newImage.Composite(frame, -cropStartX, 0, CompositeOperator.Copy);
                            newImage.Extent(new MagickGeometry((uint)copyWidth, (uint)newHeight), Gravity.North);

                            newImage.AnimationDelay = originalDelay;
                            labelSt.Text = "Add frame...";
                            Application.DoEvents();

                            partCollection.Add(newImage);

                            currentStep++;
                            progressBar.Value = (int)((double)currentStep / totalSteps * 100);
                            Application.DoEvents();
                        }

                        string outputFile = $"{Path.GetFileNameWithoutExtension(inputFilePath)}_Part{i + 1}.gif";
                        string outputDir = Path.GetDirectoryName(inputFilePath);
                        string outputPath = Path.Combine(outputDir, outputFile);

                        partCollection.Optimize();
                        labelSt.Text = "Compressing...";
                        Application.DoEvents();
                        foreach (var frame in partCollection)
                        {
                            frame.Settings.SetDefine("compress", "LZW");
                        }

                        currentStep++;
                        progressBar.Value = (int)((double)currentStep / totalSteps * 100);
                        labelSt.Text = "Saving...";
                        Application.DoEvents();

                        partCollection.Write(outputPath);

                        currentStep++;
                        progressBar.Value = (int)((double)currentStep / totalSteps * 100);
                        Application.DoEvents();

                        ModifyGifFile(outputPath, canvasHeight);
                    }
                }
            }
        }
        private static void ModifyGifFile(string filePath, int adjustedHeight)
        {
            byte[] fileData = File.ReadAllBytes(filePath);

            fileData[fileData.Length - 1] = 0x21;

            ushort heightValue = (ushort)adjustedHeight;
            fileData[8] = (byte)(heightValue & 0xFF);
            fileData[9] = (byte)((heightValue >> 8) & 0xFF);

            File.WriteAllBytes(filePath, fileData);
        }

        private static void ShowLoadingForm(Action action, Form parentForm)
        {
            Form loadingForm = new Form
            {
                Text = "Loading...",
                Width = 300,
                Height = 100,
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                ControlBox = false, // Disable close button
                Owner = parentForm
            };

            Label loadingLabel = new Label
            {
                Text = "Processing GIF file, please wait...",
                AutoSize = false,
                TextAlign = System.Drawing.ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill
            };

            loadingForm.Controls.Add(loadingLabel);

            // Show the loading form and execute the action
            loadingForm.Shown += (s, e) =>
            {
                try
                {
                    action.Invoke(); // Execute the provided action
                }
                finally
                {
                    loadingForm.Close(); // Close the loading form
                }
            };
            Application.DoEvents();
            loadingForm.ShowDialog(parentForm); // Show as modal
        }
        public static void ResizeGifTo766(GifToolMainForm mainForm)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "GIF Files (*.gif)|*.gif",
                Title = "Select a GIF file to resize"
            };

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                string inputFilePath = openFileDialog.FileName;

                // Generate output file name
                string directory = Path.GetDirectoryName(inputFilePath);
                string originalFileName = Path.GetFileNameWithoutExtension(inputFilePath);
                string outputFilePath = Path.Combine(directory, $"{originalFileName}_766px.gif");

                try
                {
                    // Update progress bar to indicate loading
                    mainForm.lblStatus.Text = "Loading...";
                    mainForm.pBarTaskStatus.Value = 0;
                    mainForm.pBarTaskStatus.Maximum = 100;
                    mainForm.pBarTaskStatus.Visible = true;
                    Application.DoEvents();

                    using (var collection = new MagickImageCollection(inputFilePath))
                    {
                        collection.Coalesce();
                        Application.DoEvents();

                        // Define total steps: resizing + LZW compression
                        int totalSteps = collection.Count * 2; // Each frame: Resize + LZW Compression
                        int currentStep = 0;

                        // Resize each frame
                        mainForm.lblStatus.Text = "Calculate frames...";
                        Application.DoEvents();
                        foreach (var frame in collection)
                        {
                            frame.ResetPage();
                            frame.Resize(766, 0);

                            // Update progress bar
                            currentStep++;
                            mainForm.pBarTaskStatus.Value = (int)((double)currentStep / totalSteps * 100);
                            Application.DoEvents();
                        }
                        mainForm.lblStatus.Text = "Compressing...";
                        Application.DoEvents();
                        // Apply LZW compression
                        foreach (var frame in collection)
                        {
                            frame.Settings.SetDefine("compress", "LZW");
                            // Update progress bar
                            currentStep++;
                            mainForm.pBarTaskStatus.Value = (int)((double)currentStep / totalSteps * 100);
                            Application.DoEvents();
                        }

                        // Write the resized GIF to the output path
                        mainForm.lblStatus.Text = "Optimization...";
                        Application.DoEvents();
                        collection.Optimize();
                        mainForm.lblStatus.Text = "Saving...";
                        Application.DoEvents();
                        collection.Write(outputFilePath);

                        // Reset progress bar
                        mainForm.pBarTaskStatus.Value = 0;
                        mainForm.lblStatus.Text = "Done.";
                        Application.DoEvents();
                        MessageBox.Show(mainForm, $"GIF resizing completed successfully!\nSaved as: {outputFilePath}",
                                        "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(mainForm, $"An error occurred: {ex.Message}",
                                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                finally
                {
                    mainForm.pBarTaskStatus.Value = 0;
                    mainForm.lblStatus.Text = "Idle.";
                    //mainForm.pBarTaskStatus.Visible = false;
                }
            }
        }
        public static void WriteTailByteForMultipleGifs(GifToolMainForm mainForm)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "GIF Files (*.gif)|*.gif",
                Title = "Select GIF files to process",
                Multiselect = true // Enable multiple file selection
            };

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                string[] filePaths = openFileDialog.FileNames;

                try
                {
                    // Set up progress bar
                    mainForm.pBarTaskStatus.Visible = true;
                    mainForm.pBarTaskStatus.Value = 0;
                    mainForm.pBarTaskStatus.Maximum = filePaths.Length;

                    int processedFiles = 0;

                    foreach (string filePath in filePaths)
                    {
                        try
                        {
                            byte[] fileData = File.ReadAllBytes(filePath);

                            if (fileData.Length > 0 && fileData[fileData.Length - 1] == 0x3B) // Check if the last byte is 0x3B
                            {
                                fileData[fileData.Length - 1] = 0x21; // Replace the last byte with 0x21
                                File.WriteAllBytes(filePath, fileData);
                            }

                            // Update progress bar
                            processedFiles++;
                            mainForm.pBarTaskStatus.Value = processedFiles;
                            Application.DoEvents();
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show(mainForm, $"Error processing file {filePath}:\n{ex.Message}",
                                            "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }

                    MessageBox.Show(mainForm, $"{processedFiles} GIF files processed successfully!",
                                    "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(mainForm, $"An error occurred: {ex.Message}",
                                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                finally
                {
                    // Reset and hide the progress bar
                    mainForm.pBarTaskStatus.Value = 0;
                    mainForm.pBarTaskStatus.Visible = false;
                }
            }
        }

    }
}
