using System;
using System.IO;
using System.Windows.Forms;
using ImageMagick;

namespace GifProcessor
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
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
                        // 獲取 GIF 整體畫布尺寸
                        uint canvasWidth = collection[0].Page.Width;
                        uint canvasHeight = collection[0].Page.Height;
                        // 判斷畫布寬度是否為 766
                        if (canvasWidth != 766)
                        {
                            string message = $"The GIF canvas width is not 766 pixels. Detected canvas dimensions: Width={canvasWidth}, Height={canvasHeight}. Please provide a valid GIF.";
                            MessageBox.Show(message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return;
                        }

                        // 初始化進度條
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
                            Text = "Processing...",
                            Width = 350,
                            Height = 80,
                            StartPosition = FormStartPosition.CenterScreen,
                            FormBorderStyle = FormBorderStyle.FixedDialog,
                            MaximizeBox = false,
                            MinimizeBox = false
                        };

                        progressBar.Left = (progressForm.ClientSize.Width - progressBar.Width) / 2;
                        progressBar.Top = (progressForm.ClientSize.Height - progressBar.Height) / 2;
                        progressForm.Controls.Add(progressBar);
                        progressForm.Shown += (s, e) => progressForm.Activate();
                        progressForm.Show();

                        // 分割 GIF
                        SplitGifIntoParts(inputFilePath, progressBar);

                        progressForm.Close();
                        MessageBox.Show("GIF processing and splitting completed successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        static void SplitGifIntoParts(string inputFilePath, ProgressBar progressBar)
        {
            using (var collection = new MagickImageCollection(inputFilePath))
            {
                // Expand all GIF frames to complete static areas
                collection.Coalesce();

                int canvasWidth = (int)collection[0].Page.Width;  // Get canvas width
                int canvasHeight = (int)collection[0].Page.Height; // Get canvas height
                int newHeight = canvasHeight + 100; // Increase height by 100px

                // Determine splitting ranges based on canvas width
                (int Start, int End)[] ranges;

                if (canvasWidth == 766)
                {
                    ranges = new (int Start, int End)[]
                    {
                (0, 149),    // First part
                (154, 303),  // Second part
                (308, 457),  // Third part
                (462, 611),  // Fourth part
                (616, canvasWidth - 1) // Fifth part
                    };
                }
                else if (canvasWidth == 774)
                {
                    ranges = new (int Start, int End)[]
                    {
                (0, 149),    // First part
                (155, 305),  // Second part
                (311, 461),  // Third part
                (467, 617),  // Fourth part
                (623, canvasWidth - 1) // Fifth part
                    };
                }
                else
                {
                    string message = $"Unsupported GIF canvas width: {canvasWidth} pixels. Only 766px or 774px are supported.";
                    MessageBox.Show(message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                int totalSteps = collection.Count * ranges.Length;
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

                            // Update the progress bar
                            currentStep++;
                            progressBar.Value = (int)((double)currentStep / totalSteps * 100);
                            Application.DoEvents();
                        }

                        // Save the split GIF file
                        string outputFileName = $"{Path.GetFileNameWithoutExtension(inputFilePath)}_Part{i + 1}.gif";
                        string outputDir = Path.GetDirectoryName(inputFilePath);
                        string outputPath = Path.Combine(outputDir, outputFileName);

                        partCollection.Optimize(); // Optimize frames
                        foreach (var frame in partCollection)
                        {
                            frame.Settings.SetDefine("compress", "LZW"); // Ensure LZW compression
                        }

                        partCollection.Write(outputPath);

                        // Modify the saved GIF file to adjust metadata
                        ModifyGifFile(outputPath, canvasHeight);
                    }
                }
            }
        }

        static void ModifyGifFile(string filePath, int adjustedHeight)
        {
            byte[] fileData = File.ReadAllBytes(filePath);

            // Set the last byte to 0x21
            fileData[fileData.Length - 1] = 0x21;

            // Write the adjusted height at byte 8 (2 bytes, little-endian)
            ushort heightValue = (ushort)adjustedHeight;
            fileData[8] = (byte)(heightValue & 0xFF);        // Lower byte
            fileData[9] = (byte)((heightValue >> 8) & 0xFF); // Higher byte

            File.WriteAllBytes(filePath, fileData);
        }
    }
}
