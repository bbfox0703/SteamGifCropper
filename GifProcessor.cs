using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

                            mainForm.lblStatus.Text = $"Processing part {i + 1}, frame {currentStep % collection.Count + 1}...";
                            Application.DoEvents();
                            
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
                                
                                // Set animation delay
                                newImage.AnimationDelay = originalDelay;
                                newImage.GifDisposeMethod = GifDisposeMethod.Background;
                                
                                partCollection.Add(newImage.Clone());
                            }

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

        public static void MergeAndSplitFiveGifs(GifToolMainForm mainForm)
        {
            // Step 1: Select five GIF files in order
            var gifFiles = SelectFiveOrderedGifs();
            if (gifFiles == null || gifFiles.Length != 5)
            {
                return; // User cancelled or didn't select exactly 5 files
            }

            try
            {
                mainForm.lblStatus.Text = "Validating and processing 5 GIF files...";
                mainForm.pBarTaskStatus.Minimum = 0;
                mainForm.pBarTaskStatus.Maximum = 100;
                mainForm.pBarTaskStatus.Value = 0;
                Application.DoEvents();

                // Step 2: Load and validate all GIF files
                var collections = LoadAndValidateGifs(gifFiles, mainForm);
                if (collections == null) return;

                UpdateProgress(mainForm.pBarTaskStatus, 20, 100);

                // Step 3: Resize GIFs to specific widths (153, 153, 154, 153, 153)
                var resizedCollections = ResizeGifsToSpecificWidths(collections, mainForm);
                UpdateProgress(mainForm.pBarTaskStatus, 40, 100);

                // Step 4: Synchronize to shortest duration
                var syncedCollections = SynchronizeToShortestDuration(resizedCollections, mainForm);
                UpdateProgress(mainForm.pBarTaskStatus, 60, 100);

                // Step 5: Merge horizontally to create 766px wide GIF
                var mergedCollection = MergeGifsHorizontally(syncedCollections, mainForm);
                UpdateProgress(mainForm.pBarTaskStatus, 80, 100);

                // Step 6: Apply existing split functionality
                // Create merged filename based on first selected GIF
                string firstGifPath = gifFiles[0];
                string mergedFileName = $"{Path.GetFileNameWithoutExtension(firstGifPath)}_merged.gif";
                string outputDir = Path.GetDirectoryName(firstGifPath);
                string mergedFilePath = Path.Combine(outputDir, mergedFileName);
                
                mergedCollection.Write(mergedFilePath);

                // Use existing split logic
                var ranges = GetCropRanges(SupportedWidth1); // Use 766px ranges
                int adjustedHeight = CalculateAdjustedHeight(mergedCollection);
                SplitGif(mergedCollection, mergedFilePath, mainForm, ranges, adjustedHeight);

                // Note: mergedFilePath is kept as the intermediate merged file

                UpdateProgress(mainForm.pBarTaskStatus, 100, 100);
                mainForm.lblStatus.Text = "Five GIF merge and split completed successfully!";
                MessageBox.Show("Five GIF files have been merged and split into 5 parts successfully!",
                              "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);

                // Dispose collections
                foreach (var collection in syncedCollections)
                {
                    collection.Dispose();
                }
                mergedCollection.Dispose();
            }
            catch (Exception ex)
            {
                mainForm.lblStatus.Text = "Error occurred during processing.";
                MessageBox.Show($"Error processing five GIF merge and split: {ex.Message}",
                              "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static string[] SelectFiveOrderedGifs()
        {
            var selectedFiles = new List<string>();
            
            MessageBox.Show("You will need to select 5 GIF files one by one in the desired order.\n\n" +
                          "File 1: First position (leftmost)\n" +
                          "File 2: Second position\n" +
                          "File 3: Third position (center)\n" +
                          "File 4: Fourth position\n" +
                          "File 5: Fifth position (rightmost)",
                          "Selection Order Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
            
            for (int i = 1; i <= 5; i++)
            {
                using (var openFileDialog = new OpenFileDialog
                {
                    Filter = "GIF Files (*.gif)|*.gif",
                    Title = $"Select GIF file #{i} (position {i} from left to right)",
                    Multiselect = false
                })
                {
                    if (openFileDialog.ShowDialog() != DialogResult.OK)
                    {
                        // User cancelled
                        return null;
                    }
                    selectedFiles.Add(openFileDialog.FileName);
                }
            }
            
            return selectedFiles.ToArray();
        }

        private static MagickImageCollection[] LoadAndValidateGifs(string[] gifFiles, GifToolMainForm mainForm)
        {
            var collections = new MagickImageCollection[5];

            try
            {
                for (int i = 0; i < 5; i++)
                {
                    collections[i] = new MagickImageCollection(gifFiles[i]);
                    
                    // Validate that GIF has palette colors (8-bit)
                    if (collections[i][0].ColorType != ColorType.Palette && collections[i][0].ColorType != ColorType.PaletteAlpha)
                    {
                        MessageBox.Show($"GIF #{i + 1} ({Path.GetFileName(gifFiles[i])}) must have palette-based colors (8-bit).\nCurrent color type: {collections[i][0].ColorType}",
                                      "Invalid Color Type", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        
                        // Cleanup loaded collections
                        for (int j = 0; j <= i; j++)
                        {
                            collections[j]?.Dispose();
                        }
                        return null;
                    }
                }
                return collections;
            }
            catch (Exception ex)
            {
                // Cleanup on error
                foreach (var collection in collections)
                {
                    collection?.Dispose();
                }
                throw new InvalidOperationException($"Failed to load and validate GIF files: {ex.Message}", ex);
            }
        }

        private static MagickImageCollection[] ResizeGifsToSpecificWidths(MagickImageCollection[] collections, GifToolMainForm mainForm)
        {
            int[] targetWidths = { 153, 153, 154, 153, 153 };
            var resizedCollections = new MagickImageCollection[5];

            try
            {
                for (int i = 0; i < 5; i++)
                {
                    mainForm.lblStatus.Text = $"Resizing GIF #{i + 1} to {targetWidths[i]}px width...";
                    Application.DoEvents();

                    resizedCollections[i] = new MagickImageCollection();
                    
                    // Coalesce for proper animation handling
                    collections[i].Coalesce();
                    
                    foreach (var frame in collections[i])
                    {
                        // Resize maintaining aspect ratio
                        frame.Resize((uint)targetWidths[i], 0);
                        resizedCollections[i].Add(frame.Clone());
                    }

                    // Copy animation settings
                    for (int j = 0; j < resizedCollections[i].Count; j++)
                    {
                        resizedCollections[i][j].AnimationDelay = collections[i][j].AnimationDelay;
                    }
                }

                return resizedCollections;
            }
            catch (Exception ex)
            {
                // Cleanup on error
                foreach (var collection in resizedCollections)
                {
                    collection?.Dispose();
                }
                throw new InvalidOperationException($"Failed to resize GIF files: {ex.Message}", ex);
            }
        }

        private static MagickImageCollection[] SynchronizeToShortestDuration(MagickImageCollection[] collections, GifToolMainForm mainForm)
        {
            mainForm.lblStatus.Text = "Synchronizing animation durations...";
            Application.DoEvents();

            // Calculate total duration for each GIF (frames * delay)
            var durations = new int[5];
            for (int i = 0; i < 5; i++)
            {
                durations[i] = (int)collections[i].Sum(frame => (long)frame.AnimationDelay);
            }

            // Find shortest duration
            int shortestDuration = durations.Min();
            int shortestIndex = Array.IndexOf(durations, shortestDuration);

            mainForm.lblStatus.Text = $"Shortest duration: {shortestDuration/100.0:F1}s (GIF #{shortestIndex + 1})";
            Application.DoEvents();

            // Synchronize all GIFs to shortest duration
            var syncedCollections = new MagickImageCollection[5];

            try
            {
                for (int i = 0; i < 5; i++)
                {
                    syncedCollections[i] = new MagickImageCollection();
                    
                    if (durations[i] == shortestDuration)
                    {
                        // Already the shortest, copy as-is
                        foreach (var frame in collections[i])
                        {
                            syncedCollections[i].Add(frame.Clone());
                        }
                    }
                    else
                    {
                        // Trim to shortest duration
                        int currentDuration = 0;
                        foreach (var frame in collections[i])
                        {
                            if (currentDuration + (int)frame.AnimationDelay <= shortestDuration)
                            {
                                syncedCollections[i].Add(frame.Clone());
                                currentDuration += (int)frame.AnimationDelay;
                            }
                            else
                            {
                                break; // Stop when we reach the shortest duration
                            }
                        }
                    }
                    
                    // Set loop animation for each frame
                    foreach (var frame in syncedCollections[i])
                    {
                        frame.GifDisposeMethod = GifDisposeMethod.Background;
                    }
                }

                return syncedCollections;
            }
            catch (Exception ex)
            {
                // Cleanup on error
                foreach (var collection in syncedCollections)
                {
                    collection?.Dispose();
                }
                throw new InvalidOperationException($"Failed to synchronize GIF durations: {ex.Message}", ex);
            }
        }

        private static MagickImageCollection MergeGifsHorizontally(MagickImageCollection[] collections, GifToolMainForm mainForm)
        {
            mainForm.lblStatus.Text = "Merging GIFs horizontally to 766px width...";
            Application.DoEvents();

            // Calculate maximum height among all resized GIFs
            int maxHeight = collections.Max(c => (int)c[0].Height);
            
            // Create merged collection
            var mergedCollection = new MagickImageCollection();
            int maxFrames = collections.Max(c => c.Count);

            try
            {
                for (int frameIndex = 0; frameIndex < maxFrames; frameIndex++)
                {
                    // Create 766px wide canvas
                    var canvas = new MagickImage(MagickColors.Transparent, 766, (uint)maxHeight);
                    
                    // X positions for each GIF: 0, 153, 306, 460, 613
                    int[] xPositions = { 0, 153, 306, 460, 613 };
                    
                    for (int gifIndex = 0; gifIndex < 5; gifIndex++)
                    {
                        // Get frame (loop if GIF has fewer frames)
                        var collection = collections[gifIndex];
                        var frameIdx = frameIndex % collection.Count;
                        var frame = collection[frameIdx];
                        
                        // Composite frame onto canvas at specific X position
                        canvas.Composite(frame, xPositions[gifIndex], 0, CompositeOperator.Over);
                    }

                    // Set animation delay (use delay from first GIF)
                    canvas.AnimationDelay = collections[0][frameIndex % collections[0].Count].AnimationDelay;
                    
                    mergedCollection.Add(canvas);
                }

                // Set infinite loop for each frame
                foreach (var frame in mergedCollection)
                {
                    frame.GifDisposeMethod = GifDisposeMethod.Background;
                }
                
                return mergedCollection;
            }
            catch (Exception ex)
            {
                mergedCollection?.Dispose();
                throw new InvalidOperationException($"Failed to merge GIFs horizontally: {ex.Message}", ex);
            }
        }

        private static int CalculateAdjustedHeight(MagickImageCollection collection)
        {
            return (int)collection[0].Height + HeightExtension;
        }

        public static void SplitGifWithReducedPalette(GifToolMainForm mainForm)
        {
            // Keep the original method name for backward compatibility
            // but redirect to the new merge and split functionality
            MergeAndSplitFiveGifs(mainForm);
        }

        [Obsolete("This method has been replaced with MergeAndSplitFiveGifs")]
        public static void SplitGifWithReducedPaletteOld(GifToolMainForm mainForm)
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

                            mainForm.lblStatus.Text = $"Processing part {i + 1} with palette reduction, frame {currentStep % collection.Count + 1}...";
                            Application.DoEvents();
                            
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

                                // Set animation properties
                                newImage.AnimationDelay = originalDelay;
                                newImage.GifDisposeMethod = GifDisposeMethod.Background;

                                // Apply palette reduction
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
        public static void RestoreTailByteForMultipleGifs(GifToolMainForm mainForm)
        {
            using (var openFileDialog = new OpenFileDialog
            {
                Filter = "GIF Files (*.gif)|*.gif",
                Title = "Select GIF files to restore tail bytes from 0x21 to 0x3B",
                Multiselect = true
            })
            {
                if (openFileDialog.ShowDialog() != DialogResult.OK) return;

                string[] selectedFiles = openFileDialog.FileNames;
                int processedCount = 0;
                int skippedCount = 0;

                try
                {
                    mainForm.lblStatus.Text = "Restoring tail bytes...";
                    mainForm.pBarTaskStatus.Value = 0;
                    mainForm.pBarTaskStatus.Maximum = selectedFiles.Length;
                    mainForm.pBarTaskStatus.Visible = true;

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
                            MessageBox.Show($"Error processing {Path.GetFileName(filePath)}: {ex.Message}",
                                          "File Processing Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            skippedCount++;
                        }

                        mainForm.pBarTaskStatus.Value++;
                        mainForm.lblStatus.Text = $"Processing... {mainForm.pBarTaskStatus.Value}/{selectedFiles.Length}";
                        Application.DoEvents();
                    }

                    string resultMessage = $"Restoration completed!\n" +
                                         $"Files processed: {processedCount}\n" +
                                         $"Files skipped (not 0x21): {skippedCount}";

                    MessageBox.Show(resultMessage, "Tail Byte Restoration", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"An error occurred: {ex.Message}",
                                  "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                finally
                {
                    mainForm.pBarTaskStatus.Value = 0;
                    mainForm.pBarTaskStatus.Visible = false;
                    mainForm.lblStatus.Text = "Ready";
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
