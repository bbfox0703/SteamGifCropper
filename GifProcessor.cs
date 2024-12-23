using System;
using System.IO;
using System.Windows.Forms;
using ImageMagick;

namespace GifProcessorApp
{
    public static class GifProcessor
    {
        public static void StartProcessing(Form parentForm)
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

                        ProgressBar progressBar = new ProgressBar
                        {
                            Minimum = 0,
                            Maximum = 100,
                            Value = 0,
                            Dock = DockStyle.None,
                            Width = 300,
                            Height = 30
                        };

                        Form progressForm = new Form
                        {
                            Owner = parentForm,
                            Text = "Processing...",
                            Width = 350,
                            Height = 100,
                            StartPosition = FormStartPosition.CenterParent,
                            FormBorderStyle = FormBorderStyle.FixedDialog,
                            MaximizeBox = false,
                            MinimizeBox = false
                        };

                        progressBar.Left = (progressForm.ClientSize.Width - progressBar.Width) / 2;
                        progressBar.Top = (progressForm.ClientSize.Height - progressBar.Height) / 2;
                        progressForm.Controls.Add(progressBar);
                        progressForm.Shown += (s, e) => progressForm.Activate();
                        progressForm.Show();

                        SplitGif(inputFilePath, progressBar, (int)canvasWidth, (int)canvasHeight);

                        progressForm.Close();
                        MessageBox.Show("GIF processing and splitting completed successfully!",
                                        "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"An error occurred: {ex.Message}",
                                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private static void SplitGif(string inputFilePath, ProgressBar progressBar, int canvasWidth, int canvasHeight)
        {
            using (var collection = new MagickImageCollection(inputFilePath))
            {
                // Expand all GIF frames to complete static areas
                collection.Coalesce();
                Application.DoEvents();
                int newHeight = canvasHeight + 100;

                (int Start, int End)[] ranges = canvasWidth == 766
                    ? new (int Start, int End)[]
                    {
                (0, 149), (154, 303), (308, 457), (462, 611), (616, canvasWidth - 1)
                    }
                    : new (int Start, int End)[]
                    {
                (0, 149), (155, 305), (311, 461), (467, 617), (623, canvasWidth - 1)
                    };

                int totalSteps = (collection.Count * ranges.Length) + (ranges.Length * 2); // Processing + LZW compression + Writing
                int currentStep = 0;

                for (int i = 0; i < ranges.Length; i++)
                {
                    using (var partCollection = new MagickImageCollection())
                    {
                        foreach (var frame in collection)
                        {
                            uint originalDelay = frame.AnimationDelay; // Preserve the original frame delay
                            int copyWidth = ranges[i].End - ranges[i].Start + 1;
                            frame.ResetPage(); // Reset frame page offsets
                            frame.Extent(new MagickGeometry((uint)canvasWidth, (uint)canvasHeight), Gravity.Northwest);

                            // Create a new image with the specified width and increased height
                            MagickImage newImage = new MagickImage(MagickColors.Transparent, (uint)copyWidth, (uint)newHeight);

                            // Calculate the X-axis offset for cropping
                            int cropStartX = ranges[i].Start;
                            if (cropStartX < 0) cropStartX = 0; // Prevent negative values

                            // Copy the relevant content to the new image
                            newImage.Composite(frame, -cropStartX, 0, CompositeOperator.Copy);

                            // Extend the height with a transparent background
                            newImage.Extent(new MagickGeometry((uint)copyWidth, (uint)newHeight), Gravity.North);

                            // Retain the animation delay
                            newImage.AnimationDelay = originalDelay;
                            partCollection.Add(newImage);

                            // Update the progress bar for frame processing
                            currentStep++;
                            progressBar.Value = (int)((double)currentStep / totalSteps * 100);
                            Application.DoEvents();
                        }

                        // Save the split GIF file
                        string outputFile = $"{Path.GetFileNameWithoutExtension(inputFilePath)}_Part{i + 1}.gif";
                        string outputDir = Path.GetDirectoryName(inputFilePath);
                        string outputPath = Path.Combine(outputDir, outputFile);

                        partCollection.Optimize();
                        foreach (var frame in partCollection)
                        {
                            frame.Settings.SetDefine("compress", "LZW"); // Ensure LZW compression
                        }

                        // Update progress bar for LZW compression
                        currentStep++;
                        progressBar.Value = (int)((double)currentStep / totalSteps * 100);
                        Application.DoEvents();

                        partCollection.Write(outputPath);

                        // Update progress bar for writing file
                        currentStep++;
                        progressBar.Value = (int)((double)currentStep / totalSteps * 100);
                        Application.DoEvents();

                        // Modify the saved GIF file to adjust metadata
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
        public static void ResizeGifTo766(Form parentForm)
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
                    // Show loading form while loading the GIF file
                    ShowLoadingForm(() =>
                    {
                        using (var collection = new MagickImageCollection(inputFilePath))
                        {
                            // Ensure all frames are coalesced
                            Application.DoEvents();
                            collection.Coalesce();
                            Application.DoEvents();

                            // Define total steps: resizing + LZW compression
                            int totalSteps = collection.Count * 2; // Each frame: Resize + LZW Compression
                            int currentStep = 0;

                            // Initialize progress bar
                            ProgressBar progressBar = new ProgressBar
                            {
                                Minimum = 0,
                                Maximum = 100,
                                Value = 0,
                                Dock = DockStyle.None,
                                Width = 300,
                                Height = 30
                            };

                            Form progressForm = new Form
                            {
                                Owner = parentForm,
                                Text = "Resizing GIF...",
                                Width = 350,
                                Height = 100,
                                StartPosition = FormStartPosition.CenterParent,
                                FormBorderStyle = FormBorderStyle.FixedDialog,
                                MaximizeBox = false,
                                MinimizeBox = false
                            };

                            progressBar.Left = (progressForm.ClientSize.Width - progressBar.Width) / 2;
                            progressBar.Top = (progressForm.ClientSize.Height - progressBar.Height) / 2;
                            progressForm.Controls.Add(progressBar);
                            progressForm.Shown += (s, e) => progressForm.Activate();
                            progressForm.Show();

                            // Resize each frame
                            foreach (var frame in collection)
                            {
                                frame.ResetPage();
                                frame.Resize(766, 0);

                                // Update progress bar
                                currentStep++;
                                progressBar.Value = (int)((double)currentStep / totalSteps * 100);
                                Application.DoEvents();
                            }

                            // Apply LZW compression
                            foreach (var frame in collection)
                            {
                                frame.Settings.SetDefine("compress", "LZW");

                                // Update progress bar
                                currentStep++;
                                progressBar.Value = (int)((double)currentStep / totalSteps * 100);
                                Application.DoEvents();
                            }

                            // Write the resized GIF to the output path
                            Application.DoEvents();
                            collection.Optimize();
                            Application.DoEvents();
                            collection.Write(outputFilePath);

                            progressForm.Close();
                            MessageBox.Show(parentForm, $"GIF resizing completed successfully!\nSaved as: {outputFilePath}",
                                            "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                    }, parentForm);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(parentForm, $"An error occurred: {ex.Message}",
                                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
    }
}
