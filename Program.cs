using System;
using System.IO;
using System.Windows.Forms;
using ImageMagick;

namespace GifResizer
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

                SaveFileDialog saveFileDialog = new SaveFileDialog
                {
                    Filter = "GIF Files (*.gif)|*.gif",
                    Title = "Save the processed GIF file"
                };

                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    string outputFilePath = saveFileDialog.FileName;

                    try
                    {
                        using (var collection = new MagickImageCollection(inputFilePath))
                        {
                            uint targetWidth = 767;
                            foreach (var frame in collection)
                            {
                                double aspectRatio = (double)frame.Height / frame.Width;
                                uint targetHeight = (uint)(targetWidth * aspectRatio);

                                frame.Resize(targetWidth, targetHeight);

                                MagickImage transparentLayer = new MagickImage(MagickColors.Transparent, targetWidth, 100);
                                frame.Extent(new MagickGeometry(targetWidth, (targetHeight + 100)), Gravity.North);
                            }

                            collection.Write(outputFilePath);
                        }

                        SplitGifIntoParts(outputFilePath);
                        MessageBox.Show("GIF processing and splitting completed successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        static void SplitGifIntoParts(string inputFilePath)
        {
            var ranges = new (int Start, int End)[]
            {
                (0, 149),   // First part
                (153, 303), // Second part
                (307, 457), // Third part
                (461, 611), // Fourth part
                (615, 765)  // Fifth part
            };

            using (var collection = new MagickImageCollection(inputFilePath))
            {
                for (int i = 0; i < ranges.Length; i++)
                {
                    using (var partCollection = new MagickImageCollection())
                    {
                        foreach (var frame in collection)
                        {
                            // Create a new blank image with the range width and original height
                            int copyWidth = ranges[i].End - ranges[i].Start + 1;
                            MagickImage newImage = new MagickImage(MagickColors.Transparent, (uint)copyWidth, frame.Height);

                            // Copy the range content to the new image
                            newImage.Composite(frame, -ranges[i].Start, 0, CompositeOperator.Copy);

                            // Get the transparent color (pixel at (0, height - 1))
                            using (var pixels = frame.GetPixels())
                            {
                                var pixel = pixels.GetPixel(0, (int)frame.Height - 1);
                                var transparentColor = pixel.ToColor(); // Convert pixel to color
                                newImage.Transparent(transparentColor);
                            }

                            // Add the new image to the collection
                            partCollection.Add(newImage);
                        }

                        // Save the new GIF file
                        string outputFileName = $"{System.IO.Path.GetFileNameWithoutExtension(inputFilePath)}_Part{i + 1}.gif";
                        string outputDir = System.IO.Path.GetDirectoryName(inputFilePath);
                        string outputPath = System.IO.Path.Combine(outputDir, outputFileName);
                        partCollection.Write(outputPath);

                        // Modify the saved GIF file
                        ModifyGifFile(outputPath, (int)collection[0].Height - 100);
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

            // Write the modified file back
            File.WriteAllBytes(filePath, fileData);
        }
    }
}
