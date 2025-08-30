using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using FFMpegCore;
using FFMpegCore.Exceptions;
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
            string message = string.Format(SteamGifCropper.Properties.Resources.Error_UnsupportedWidth, width, SupportedWidth1, SupportedWidth2);
            MessageBox.Show(message, SteamGifCropper.Properties.Resources.Title_Error, MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                Filter = SteamGifCropper.Properties.Resources.FileDialog_GifFilter,
                Title = SteamGifCropper.Properties.Resources.FileDialog_SelectGif
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

                        mainForm.lblStatus.Text = SteamGifCropper.Properties.Resources.Status_Processing;
                        mainForm.pBarTaskStatus.Minimum = 0;
                        mainForm.pBarTaskStatus.Maximum = 100;
                        mainForm.pBarTaskStatus.Value = 0;
                        Application.DoEvents();

                        var ranges = GetCropRanges(canvasWidth);
                        int targetFramerate = (int)mainForm.numUpDownFramerate.Value;
                        SplitGif(collection, inputFilePath, mainForm, ranges, (int)canvasHeight, targetFramerate);
                        mainForm.lblStatus.Text = SteamGifCropper.Properties.Resources.Status_Done;
                        MessageBox.Show(SteamGifCropper.Properties.Resources.Message_ProcessingComplete,
                                        SteamGifCropper.Properties.Resources.Title_Success, MessageBoxButtons.OK, MessageBoxIcon.Information);

                    }
                }
                catch (Exception ex)
                {
                    mainForm.lblStatus.Text = SteamGifCropper.Properties.Resources.Status_Error;
                    MessageBox.Show(string.Format(SteamGifCropper.Properties.Resources.Error_Occurred, ex.Message),
                                    SteamGifCropper.Properties.Resources.Title_Error, MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                finally
                {
                    mainForm.pBarTaskStatus.Value = 0;
                    mainForm.lblStatus.Text = SteamGifCropper.Properties.Resources.Status_Idle;
                }
            }
        }
        private static void SplitGif(MagickImageCollection collection, string inputFilePath, GifToolMainForm mainForm, (int Start, int End)[] ranges, int canvasHeight, int targetFramerate = 15)
        {
            mainForm.lblStatus.Text = SteamGifCropper.Properties.Resources.Status_CoalescingFrames;
            Application.DoEvents();
            collection.Coalesce();
            Application.DoEvents();
            int newHeight = canvasHeight + HeightExtension;
            
            // Calculate target frame delay in centiseconds (1/100th of a second)
            // For 15fps: 100/15 ≈ 6.67 centiseconds, rounded to 7
            uint targetDelay = (uint)Math.Round(100.0 / targetFramerate);

            int totalSteps = (collection.Count * ranges.Length) + (ranges.Length * 2);
            int currentStep = 0;

                for (int i = 0; i < ranges.Length; i++)
                {
                    using (var partCollection = new MagickImageCollection())
                    {
                        foreach (var frame in collection)
                        {
                            int copyWidth = ranges[i].End - ranges[i].Start + 1;

                            mainForm.lblStatus.Text = string.Format(SteamGifCropper.Properties.Resources.Status_ProcessingPart, i + 1, currentStep % collection.Count + 1);
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
                                
                                // Set animation delay to target framerate
                                newImage.AnimationDelay = targetDelay;
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
                        mainForm.lblStatus.Text = SteamGifCropper.Properties.Resources.Status_Compressing;
                        Application.DoEvents();
                        int compressFrameCount = 0;
                        foreach (var frame in partCollection)
                        {
                            frame.Settings.SetDefine("compress", "LZW");
                            
                            // Update every 25 frames during compression
                            if (++compressFrameCount % 25 == 0)
                            {
                                Application.DoEvents();
                            }
                        }

                        currentStep++;
                        UpdateProgress(mainForm.pBarTaskStatus, currentStep, totalSteps);
                        mainForm.lblStatus.Text = SteamGifCropper.Properties.Resources.Status_Saving;
                        Application.DoEvents();

                        partCollection.Write(outputPath);

                        if (mainForm.chkGifsicle.Checked)
                        {
                            mainForm.lblStatus.Text = SteamGifCropper.Properties.Resources.Status_GifsicleOptimizing;
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

            // Validate all source files exist
            foreach (string gifPath in gifFiles)
            {
                if (!File.Exists(gifPath))
                {
                    MessageBox.Show($"Source file not found: {Path.GetFileName(gifPath)}", 
                                   SteamGifCropper.Properties.Resources.Title_Error, 
                                   MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }

            try
            {
                mainForm.lblStatus.Text = SteamGifCropper.Properties.Resources.Status_ValidatingProcessing;
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
                bool useFastPalette = mainForm.chk5GIFMergeFasterPaletteProcess.Checked;
                var mergedCollection = MergeGifsHorizontally(syncedCollections, mainForm, useFastPalette);
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
                int targetFramerate = (int)mainForm.numUpDownFramerate.Value;
                SplitGif(mergedCollection, mergedFilePath, mainForm, ranges, adjustedHeight, targetFramerate);

                // Note: mergedFilePath is kept as the intermediate merged file

                UpdateProgress(mainForm.pBarTaskStatus, 100, 100);
                mainForm.lblStatus.Text = "Five GIF merge and split completed successfully!";
                MessageBox.Show("Five GIF files have been merged and split into 5 parts successfully!",
                              SteamGifCropper.Properties.Resources.Title_Success, MessageBoxButtons.OK, MessageBoxIcon.Information);

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
                              SteamGifCropper.Properties.Resources.Title_Error, MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                    
                    int frameCount = 0;
                    foreach (var frame in collections[i])
                    {
                        // Resize maintaining aspect ratio
                        frame.Resize((uint)targetWidths[i], 0);
                        resizedCollections[i].Add(frame.Clone());
                        
                        // Update UI every 10 frames to keep responsive
                        if (++frameCount % 10 == 0)
                        {
                            mainForm.lblStatus.Text = $"Resizing GIF #{i + 1} - Frame {frameCount}/{collections[i].Count}...";
                            Application.DoEvents();
                        }
                    }

                    // Copy animation settings
                    for (int j = 0; j < resizedCollections[i].Count; j++)
                    {
                        resizedCollections[i][j].AnimationDelay = collections[i][j].AnimationDelay;
                        
                        // Update UI every 50 frames for animation settings
                        if (j % 50 == 0 && j > 0)
                        {
                            Application.DoEvents();
                        }
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
                    mainForm.lblStatus.Text = $"Synchronizing GIF #{i + 1} duration...";
                    Application.DoEvents();
                    
                    syncedCollections[i] = new MagickImageCollection();
                    
                    if (durations[i] == shortestDuration)
                    {
                        // Already the shortest, copy as-is
                        int frameCount = 0;
                        foreach (var frame in collections[i])
                        {
                            syncedCollections[i].Add(frame.Clone());
                            
                            // Update every 20 frames
                            if (++frameCount % 20 == 0)
                            {
                                Application.DoEvents();
                            }
                        }
                    }
                    else
                    {
                        // Trim to shortest duration
                        int currentDuration = 0;
                        int frameCount = 0;
                        foreach (var frame in collections[i])
                        {
                            if (currentDuration + (int)frame.AnimationDelay <= shortestDuration)
                            {
                                syncedCollections[i].Add(frame.Clone());
                                currentDuration += (int)frame.AnimationDelay;
                                
                                // Update every 20 frames
                                if (++frameCount % 20 == 0)
                                {
                                    Application.DoEvents();
                                }
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

        private static MagickImageCollection MergeGifsHorizontally(MagickImageCollection[] collections, GifToolMainForm mainForm, bool useFastPalette = false)
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
                    // Update UI every 10 frames during merging
                    if (frameIndex % 10 == 0)
                    {
                        mainForm.lblStatus.Text = $"Merging frame {frameIndex + 1}/{maxFrames}...";
                        Application.DoEvents();
                    }
                    
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

                // Apply palette integration
                mainForm.lblStatus.Text = useFastPalette ? 
                    "Fast palette integration..." : 
                    "Integrating palettes...";
                Application.DoEvents();

                // Apply palette optimization (integrate palettes from multiple sources)
                var quantizeSettings = new QuantizeSettings
                {
                    Colors = 256, // Use 256 as maximum but allow fewer
                    ColorSpace = ColorSpace.RGB,
                    DitherMethod = useFastPalette ? DitherMethod.No : DitherMethod.FloydSteinberg
                };
                
                if (useFastPalette)
                {
                    quantizeSettings.TreeDepth = 6; // Faster but lower quality
                }

                mergedCollection.Quantize(quantizeSettings);
                
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

        public static void SplitGif(string inputFilePath, string outputDirectory, int targetFramerate = 15)
        {
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
            uint targetDelay = (uint)Math.Round(100.0 / targetFramerate);

            Directory.CreateDirectory(outputDirectory);

            for (int i = 0; i < ranges.Length; i++)
            {
                using var partCollection = new MagickImageCollection();
                foreach (var frame in collection)
                {
                    int copyWidth = ranges[i].End - ranges[i].Start + 1;

                    using var newImage = new MagickImage(MagickColors.Transparent, (uint)copyWidth, (uint)newHeight);
                    var cropGeometry = new MagickGeometry(ranges[i].Start, 0, (uint)copyWidth, (uint)canvasHeight);
                    using var croppedFrame = frame.Clone();
                    croppedFrame.Crop(cropGeometry);
                    croppedFrame.ResetPage();
                    newImage.Composite(croppedFrame, 0, 0, CompositeOperator.Over);
                    newImage.AnimationDelay = targetDelay;
                    newImage.GifDisposeMethod = GifDisposeMethod.Background;
                    partCollection.Add(newImage.Clone());
                }

                partCollection.Optimize();
                foreach (var frame in partCollection)
                {
                    frame.Settings.SetDefine("compress", "LZW");
                }

                string outputFile = Path.Combine(outputDirectory, $"{Path.GetFileNameWithoutExtension(inputFilePath)}_Part{i + 1}.gif");
                partCollection.Write(outputFile);
                ModifyGifFile(outputFile, canvasHeight);
            }
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
                            MessageBox.Show("Palette size must be between 32 and 256.", SteamGifCropper.Properties.Resources.Title_Error, MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return;
                        }

                        mainForm.lblStatus.Text = "Processing with reduced palette...";
                        mainForm.pBarTaskStatus.Minimum = 0;
                        mainForm.pBarTaskStatus.Maximum = 100;
                        mainForm.pBarTaskStatus.Value = 0;
                        Application.DoEvents();

                        var ranges = GetCropRanges(canvasWidth);
                        int targetFramerate = (int)mainForm.numUpDownFramerate.Value;
                        ReducePaletteAndSplitGif(inputFilePath, mainForm, ranges, (int)canvasHeight, paletteSize, targetFramerate);
                        mainForm.lblStatus.Text = "Done.";
                        MessageBox.Show("GIF processing with reduced palette completed successfully!",
                                        SteamGifCropper.Properties.Resources.Title_Success, MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
                catch (Exception ex)
                {
                    mainForm.lblStatus.Text = SteamGifCropper.Properties.Resources.Status_Error;
                    MessageBox.Show(string.Format(SteamGifCropper.Properties.Resources.Error_Occurred, ex.Message),
                                    SteamGifCropper.Properties.Resources.Title_Error, MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                finally
                {
                    mainForm.pBarTaskStatus.Value = 0;
                    mainForm.lblStatus.Text = SteamGifCropper.Properties.Resources.Status_Idle;
                }
            }
        }

        private static void ReducePaletteAndSplitGif(string inputFilePath, GifToolMainForm mainForm, (int Start, int End)[] ranges, int canvasHeight, int paletteSize, int targetFramerate = 15)
        {
            using (var collection = new MagickImageCollection(inputFilePath))
            {
                mainForm.lblStatus.Text = SteamGifCropper.Properties.Resources.Status_CoalescingFrames;
                Application.DoEvents();
                collection.Coalesce();
                Application.DoEvents();

                int newHeight = canvasHeight + HeightExtension;
                
                // Calculate target frame delay in centiseconds (1/100th of a second)
                uint targetDelay = (uint)Math.Round(100.0 / targetFramerate);

                int totalSteps = (collection.Count * ranges.Length) + (ranges.Length * 3); // Processing + Palette reduction + LZW compression
                int currentStep = 0;

                for (int i = 0; i < ranges.Length; i++)
                {
                    using (var partCollection = new MagickImageCollection())
                    {
                        foreach (var frame in collection)
                        {
                            int copyWidth = ranges[i].End - ranges[i].Start + 1;

                            mainForm.lblStatus.Text = string.Format(SteamGifCropper.Properties.Resources.Status_ProcessingPartPalette, i + 1, currentStep % collection.Count + 1);
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

                                // Set animation properties to target framerate
                                newImage.AnimationDelay = targetDelay;
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
                        mainForm.lblStatus.Text = SteamGifCropper.Properties.Resources.Status_Compressing;
                        Application.DoEvents();
                        int compressFrameCount = 0;
                        foreach (var frame in partCollection)
                        {
                            frame.Settings.SetDefine("compress", "LZW");
                            
                            // Update every 25 frames during compression
                            if (++compressFrameCount % 25 == 0)
                            {
                                Application.DoEvents();
                            }
                        }

                        currentStep++;
                        UpdateProgress(mainForm.pBarTaskStatus, currentStep, totalSteps);
                        mainForm.lblStatus.Text = SteamGifCropper.Properties.Resources.Status_Saving;
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
        public static void ResizeGifTo766(string inputFilePath, string outputFilePath)
        {
            using (var collection = new MagickImageCollection(inputFilePath))
            {
                collection.Coalesce();

                foreach (var frame in collection)
                {
                    frame.ResetPage();
                    frame.Resize(SupportedWidth1, 0);
                    frame.Settings.SetDefine("compress", "LZW");
                }

                collection.Optimize();
                collection.Write(outputFilePath);
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
                    mainForm.lblStatus.Text = SteamGifCropper.Properties.Resources.Status_Loading;
                    mainForm.pBarTaskStatus.Value = 0;
                    mainForm.pBarTaskStatus.Maximum = 100;
                    mainForm.pBarTaskStatus.Visible = true;
                    Application.DoEvents();

                    ResizeGifTo766(inputFilePath, outputFilePath);

                    MessageBox.Show(mainForm, $"GIF resizing completed successfully!\nSaved as: {Path.GetFileName(outputFilePath)}",
                                    SteamGifCropper.Properties.Resources.Title_Success, MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(mainForm, $"Resize operation failed: {ex.Message}",
                                    SteamGifCropper.Properties.Resources.Title_Error, MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                finally
                {
                    mainForm.pBarTaskStatus.Value = 0;
                    mainForm.lblStatus.Text = SteamGifCropper.Properties.Resources.Status_Idle;
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
                    MessageBox.Show(string.Format(SteamGifCropper.Properties.Resources.Error_Occurred, ex.Message),
                                  SteamGifCropper.Properties.Resources.Title_Error, MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                finally
                {
                    mainForm.pBarTaskStatus.Value = 0;
                    mainForm.pBarTaskStatus.Visible = false;
                    mainForm.lblStatus.Text = SteamGifCropper.Properties.Resources.Status_Ready;
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
                                            SteamGifCropper.Properties.Resources.Title_Error, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }
                    }

                    MessageBox.Show(mainForm, $"{processedFiles} of {filePaths.Length} GIF files processed successfully!",
                                    SteamGifCropper.Properties.Resources.Title_Success, MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(mainForm, string.Format(SteamGifCropper.Properties.Resources.Error_Occurred, ex.Message),
                                    SteamGifCropper.Properties.Resources.Title_Error, MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                finally
                {
                    mainForm.pBarTaskStatus.Value = 0;
                    mainForm.pBarTaskStatus.Visible = false;
                    mainForm.lblStatus.Text = SteamGifCropper.Properties.Resources.Status_Idle;
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

        public static async Task MergeMultipleGifs(List<string> gifPaths, string outputPath, GifToolMainForm mainForm, int targetFramerate = 15, bool useFastPalette = false)
        {
            if (gifPaths == null || gifPaths.Count < 2 || gifPaths.Count > 5)
            {
                throw new ArgumentException(SteamGifCropper.Properties.Resources.Message_GifFileCount);
            }

            // Validate source files and destination path
            foreach (string gifPath in gifPaths)
            {
                if (!File.Exists(gifPath))
                {
                    throw new FileNotFoundException($"Source file not found: {Path.GetFileName(gifPath)}");
                }
            }
            
            var outputDir = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
            {
                try
                {
                    Directory.CreateDirectory(outputDir);
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Cannot create output directory: {ex.Message}");
                }
            }

            var collections = new List<MagickImageCollection>();
            
            try
            {
                mainForm.lblStatus.Text = SteamGifCropper.Properties.Resources.Message_AnalyzingGifs;
                await Task.Delay(1); // Allow UI update
                Application.DoEvents();
                var widths = new List<int>();
                int minFrameCount = int.MaxValue;
                double shortestDuration = double.MaxValue;

                // Load all GIFs and analyze properties
                foreach (string gifPath in gifPaths)
                {
                    var collection = new MagickImageCollection(gifPath);
                    collection.Coalesce();
                    collections.Add(collection);
                    
                    int width = (int)collection[0].Width;
                    widths.Add(width);
                    
                    // Calculate total duration
                    double totalDuration = collection.Sum(frame => frame.AnimationDelay) / 100.0; // Convert to seconds
                    if (totalDuration < shortestDuration)
                    {
                        shortestDuration = totalDuration;
                    }
                    
                    minFrameCount = Math.Min(minFrameCount, collection.Count);
                }

                // Calculate target frame count based on shortest duration and target framerate
                int targetFrameCount = Math.Max(1, (int)(shortestDuration * targetFramerate));
                
                // Calculate target delay in centiseconds
                uint targetDelay = (uint)Math.Round(100.0 / targetFramerate);

                // Calculate total width
                int totalWidth = widths.Sum();
                int maxHeight = collections.Max(c => (int)c[0].Height);

                mainForm.lblStatus.Text = SteamGifCropper.Properties.Resources.Message_MergingGifs;
                await Task.Delay(1); // Allow UI update
                Application.DoEvents();

                var mergedCollection = new MagickImageCollection();

                try
                {
                    for (int frameIndex = 0; frameIndex < targetFrameCount; frameIndex++)
                    {
                        // Update progress and allow UI updates
                        if (frameIndex % 5 == 0)
                        {
                            mainForm.lblStatus.Text = $"{SteamGifCropper.Properties.Resources.Message_MergingGifs} ({frameIndex + 1}/{targetFrameCount})";
                            await Task.Delay(1); // Allow UI update
                            Application.DoEvents();
                        }

                        // Create canvas with total width
                        var canvas = new MagickImage(MagickColors.Transparent, (uint)totalWidth, (uint)maxHeight);

                        int currentX = 0;
                        
                        for (int gifIndex = 0; gifIndex < collections.Count; gifIndex++)
                        {
                            var collection = collections[gifIndex];
                            
                            // Calculate which frame to use based on shortest duration
                            double frameProgress = (double)frameIndex / targetFrameCount;
                            int sourceFrameIndex = Math.Min((int)(frameProgress * collection.Count), collection.Count - 1);
                            
                            var frame = collection[sourceFrameIndex];
                            
                            // Composite frame onto canvas at current X position
                            canvas.Composite(frame, currentX, 0, CompositeOperator.Over);
                            
                            currentX += widths[gifIndex];
                        }

                        // Set animation delay to target framerate
                        canvas.AnimationDelay = targetDelay;
                        canvas.GifDisposeMethod = GifDisposeMethod.Background;
                        
                        mergedCollection.Add(canvas);
                    }

                    // Apply palette optimization
                    mainForm.lblStatus.Text = useFastPalette ? 
                        "Fast palette reduction..." : 
                        SteamGifCropper.Properties.Resources.Status_ReducingPalette;
                    await Task.Delay(1); // Allow UI update
                    Application.DoEvents();

                    var quantizeSettings = new QuantizeSettings
                    {
                        Colors = 256,
                        ColorSpace = ColorSpace.RGB,
                        DitherMethod = useFastPalette ? DitherMethod.No : DitherMethod.FloydSteinberg
                    };
                    
                    if (useFastPalette)
                    {
                        quantizeSettings.TreeDepth = 6; // Faster but lower quality
                    }

                    mergedCollection.Quantize(quantizeSettings);

                    // Apply LZW compression
                    foreach (var frame in mergedCollection)
                    {
                        frame.Format = MagickFormat.Gif;
                        frame.Settings.SetDefine(MagickFormat.Gif, "optimize-transparency", "true");
                    }

                    // Save the merged GIF
                    mainForm.lblStatus.Text = SteamGifCropper.Properties.Resources.Status_Saving;
                    await Task.Delay(1); // Allow UI update
                    Application.DoEvents();
                    
                    mergedCollection.Write(outputPath);

                    string successMessage = string.Format(SteamGifCropper.Properties.Resources.Message_GifMergeComplete, outputPath);
                    MessageBox.Show(successMessage, SteamGifCropper.Properties.Resources.Title_Success, MessageBoxButtons.OK, MessageBoxIcon.Information);

                    mainForm.lblStatus.Text = SteamGifCropper.Properties.Resources.Status_Done;
                }
                finally
                {
                    mergedCollection?.Dispose();
                }
            }
            catch (Exception ex)
            {
                string errorMessage = $"Error merging GIF files: {ex.Message}";
                MessageBox.Show(errorMessage, SteamGifCropper.Properties.Resources.Title_MergeGifError, MessageBoxButtons.OK, MessageBoxIcon.Error);
                mainForm.lblStatus.Text = SteamGifCropper.Properties.Resources.Status_Error;
                throw;
            }
            finally
            {
                // Clean up collections
                if (collections != null)
                {
                    foreach (var collection in collections)
                    {
                        collection?.Dispose();
                    }
                }
            }
        }

        public static async Task ConvertMp4ToGif(GifToolMainForm mainForm)
        {
            // Check if FFmpeg is available with detailed diagnostics
            var (isAvailable, ffmpegPath, ffmpegVersion, error) = GetFFmpegDiagnostics();
            
            if (!isAvailable)
            {
                string diagMessage = "FFmpeg is not installed or not available in the system PATH.\n\n";
                diagMessage += $"FFmpeg Path: {ffmpegPath ?? "Not found"}\n";
                diagMessage += $"Version: {ffmpegVersion ?? "N/A"}\n";
                if (!string.IsNullOrEmpty(error))
                    diagMessage += $"Error: {error}\n";
                
                diagMessage += "\nTo install FFmpeg:\n";
                diagMessage += "1. Open Command Prompt or PowerShell as Administrator\n";
                diagMessage += "2. Run: winget install ffmpeg\n";
                diagMessage += "3. Restart this application\n\n";
                diagMessage += "For more help, click the link in the MP4 to GIF dialog.";
                
                MessageBox.Show(diagMessage, "FFmpeg Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
                var useGPU = conversionDialog.UseGPUAcceleration;
                var targetFramerate = (int)mainForm.numUpDownFramerate.Value;

                try
                {
                    mainForm.pBarTaskStatus.Visible = true;
                    mainForm.pBarTaskStatus.Value = 0;
                    Application.DoEvents();

                    if (useGPU)
                    {
                        // For short clips or small files, CPU is often faster due to GPU memory overhead
                        bool preferCpu = duration.TotalSeconds < 8;
                        if (preferCpu)
                        {
                            mainForm.lblStatus.Text = "Short clip - CPU more efficient...";
                            useGPU = false;
                        }
                        else
                        {
                            mainForm.lblStatus.Text = "GPU decode + CPU GIF encoding...";
                        }
                        Application.DoEvents();

                        if (useGPU)
                        {
                            try
                            {
                            // Determine the source codec to choose the appropriate CUDA decoder
                            string decoder = null;
                            try
                            {
                                var analysis = await FFProbe.AnalyseAsync(inputPath);
                                var codec = analysis.PrimaryVideoStream?.CodecName?.ToLowerInvariant();
                                decoder = codec switch
                                {
                                    "h264" => "h264_cuvid",
                                    "hevc" => "hevc_cuvid",
                                    "h265" => "hevc_cuvid",
                                    "mpeg2video" => "mpeg2_cuvid",
                                    "mpeg4" => "mpeg4_cuvid",
                                    "vp9" => "vp9_cuvid",
                                    _ => null
                                };
                            }
                            catch
                            {
                                // If ffprobe fails, continue without specifying decoder
                                decoder = null;
                            }

                            // GPU-accelerated MP4 decoding, CPU GIF encoding
                            // Note: GIF encoding doesn't support CUDA, only decoding can be accelerated
                            await FFMpegArguments
                                .FromFileInput(inputPath, true, input =>
                                {
                                    input.WithCustomArgument("-hwaccel cuda");
                                    if (!string.IsNullOrEmpty(decoder))
                                        input.WithCustomArgument($"-c:v {decoder}");
                                    input.WithCustomArgument("-hwaccel_output_format cuda");
                                })
                                .OutputToFile(outputPath, true, options =>
                                {
                                    options.Seek(startTime)
                                           .WithDuration(duration)
                                           // GPU-to-CPU transfer with proper format conversion
                                           .WithCustomArgument("-vf hwdownload,format=nv12,format=rgb24")
                                           .WithFramerate(targetFramerate)
                                           .WithCustomArgument("-pix_fmt rgb8");
                                })
                                .ProcessAsynchronously();
                        }
                            catch (Exception gpuEx)
                            {
                                mainForm.lblStatus.Text = "GPU decode failed, using CPU...";
                                Application.DoEvents();

                            if (gpuEx is FFMpegException ffmpegException && !string.IsNullOrWhiteSpace(ffmpegException.FFMpegErrorOutput))
                            {
                                string logFilePath = null;
                                try
                                {
                                    string logDirectory = Path.GetDirectoryName(outputPath);
                                    if (string.IsNullOrEmpty(logDirectory) || !Directory.Exists(logDirectory))
                                        logDirectory = Path.GetTempPath();

                                    logFilePath = Path.Combine(logDirectory, "ffmpeg_gpu_error.log");
                                    File.WriteAllText(logFilePath, ffmpegException.FFMpegErrorOutput);
                                    mainForm.lblStatus.Text += $" (FFmpeg log: {logFilePath})";
                                }
                                catch
                                {
                                    string truncated = ffmpegException.FFMpegErrorOutput.Length > 200
                                        ? ffmpegException.FFMpegErrorOutput.Substring(0, 200) + "..."
                                        : ffmpegException.FFMpegErrorOutput;
                                    mainForm.lblStatus.Text += $" FFmpeg output: {truncated}";
                                }
                            }

                                // GPU failed, fallback to optimized CPU processing
                                await ProcessWithOptimizedCpu(inputPath, outputPath, startTime, duration, targetFramerate);
                            }
                        }
                    }
                    
                    if (!useGPU)
                    {
                        mainForm.lblStatus.Text = "Converting MP4 to GIF with optimized CPU...";
                        Application.DoEvents();
                        await ProcessWithOptimizedCpu(inputPath, outputPath, startTime, duration, targetFramerate);
                    }

                    mainForm.pBarTaskStatus.Value = 100;
                    mainForm.lblStatus.Text = "MP4 to GIF conversion completed successfully!";
                    
                    MessageBox.Show($"MP4 to GIF conversion completed successfully!\nSaved as: {Path.GetFileName(outputPath)}",
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
                        userFriendlyMessage = "FFmpeg executable not found. Please ensure FFmpeg is installed and available in your system PATH.\n\n" +
                                            "Installation: winget install ffmpeg";
                    }
                    else if (ex.Message.Contains("Invalid data found") || ex.Message.Contains("moov atom not found"))
                    {
                        userFriendlyMessage = "The input video file appears to be corrupted or incomplete.\n\n" +
                                            "Try using a different video file or re-download the original.";
                    }
                    else if (ex.Message.Contains("cuda") || ex.Message.Contains("nvdec"))
                    {
                        userFriendlyMessage = "GPU acceleration failed. The conversion will automatically retry with CPU processing.\n\n" +
                                            "This is normal if your GPU drivers are outdated or CUDA is not properly installed.";
                    }
                    else if (ex.Message.Contains("Permission denied") || ex.Message.Contains("Access is denied"))
                    {
                        userFriendlyMessage = "Cannot access the output directory. Please check file permissions and ensure the directory is writable.";
                    }
                    else
                    {
                        userFriendlyMessage = "An unexpected error occurred during MP4 to GIF conversion.";
                    }

                    // Append FFmpeg stderr details
                    if (!string.IsNullOrEmpty(ffmpegOutput))
                    {
                        if (!string.IsNullOrEmpty(logFilePath))
                        {
                            userFriendlyMessage += $"\n\nDetailed FFmpeg output saved to: {logFilePath}";
                        }
                        else
                        {
                            string truncated = ffmpegOutput.Length > 500 ? ffmpegOutput.Substring(0, 500) + "..." : ffmpegOutput;
                            userFriendlyMessage += $"\n\nFFmpeg output (truncated):\n{truncated}";
                        }
                    }

                    MessageBox.Show($"{userFriendlyMessage}\n\n" +
                                  $"Input file: \"{inputPath}\"\n" +
                                  $"Output file: \"{outputPath}\"\n" +
                                  $"Start time: {startTime}\n" +
                                  $"Duration: {duration}\n\n" +
                                  $"Technical details: {ex.Message}",
                                  "MP4 to GIF Conversion Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                finally
                {
                    mainForm.pBarTaskStatus.Value = 0;
                    mainForm.pBarTaskStatus.Visible = false;
                    mainForm.lblStatus.Text = SteamGifCropper.Properties.Resources.Status_Ready;
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
            // Optimized CPU processing - single-pass with minimal overhead
            await FFMpegArguments
                .FromFileInput(inputPath)
                .OutputToFile(outputPath, true, options => options
                    .Seek(startTime)
                    .WithDuration(duration)
                    .WithFramerate(targetFramerate)
                    .WithCustomArgument("-pix_fmt rgb8")
                    .WithCustomArgument("-an")) // Remove audio for faster processing
                .ProcessAsynchronously();
        }

        private static (bool isAvailable, string ffmpegPath, string version, string error) GetFFmpegDiagnostics()
        {
            try
            {
                // First, try to find FFmpeg in PATH
                string ffmpegPath = "ffmpeg"; // Will use PATH lookup
                
                var process = new Process
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
                    try { process.Kill(); } catch { }
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

                mainForm.lblStatus.Text = SteamGifCropper.Properties.Resources.Status_ReversingGif ?? "Reversing GIF...";
                mainForm.pBarTaskStatus.Visible = true;
                mainForm.pBarTaskStatus.Value = 0;

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

                    mainForm.pBarTaskStatus.Value = 25;
                    await Task.Delay(1);

                    // Use FFMpegCore to reverse the GIF
                    var inputAnalysis = await FFProbe.AnalyseAsync(inputFilePath);
                    var totalDuration = inputAnalysis.Duration;

                    mainForm.pBarTaskStatus.Value = 50;
                    await Task.Delay(1);
                    // Reverse GIF directly with palettegen + paletteuse
                    mainForm.pBarTaskStatus.Value = 75;
                    await FFMpegArguments
                        .FromFileInput(inputFilePath) // Only one input
                        .OutputToFile(
                            outputFilePath,
                            overwrite: true,
                            options => options
                                .WithCustomArgument(
                                    @"-filter_complex ""[0:v]reverse,split[s0][s1];[s0]palettegen=stats_mode=single[p];[s1][p]paletteuse=dither=bayer:bayer_scale=3"""
                                )
                                .WithFramerate(targetFramerate)
                        )
                        .ProcessAsynchronously();

                    mainForm.pBarTaskStatus.Value = 100;
                    await Task.Delay(1);

                    mainForm.lblStatus.Text = SteamGifCropper.Properties.Resources.Status_GifReversed ?? "GIF reversed successfully!";
                    MessageBox.Show(
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
                        mainForm.lblStatus.Text = "FFmpeg failed, trying ImageMagick fallback...";
                        mainForm.pBarTaskStatus.Value = 25;
                        
                        // Get target framerate from main form
                        int fallbackFramerate = (int)mainForm.numUpDownFramerate.Value;
                        
                        using (var collection = new MagickImageCollection(inputFilePath))
                        {
                            mainForm.pBarTaskStatus.Value = 50;
                            await Task.Delay(1);
                            
                            // Reverse the frame order
                            collection.Reverse();
                            
                            mainForm.pBarTaskStatus.Value = 75;
                            await Task.Delay(1);
                            
                            // Apply framerate setting to all frames
                            uint frameDelay = (uint)(100.0 / fallbackFramerate); // Convert fps to delay (in 1/100th seconds)
                            foreach (var frame in collection)
                            {
                                frame.AnimationDelay = frameDelay;
                            }
                            
                            mainForm.pBarTaskStatus.Value = 90;
                            await Task.Delay(1);
                            
                            collection.Write(outputFilePath);
                            
                            mainForm.pBarTaskStatus.Value = 100;
                            mainForm.lblStatus.Text = SteamGifCropper.Properties.Resources.Status_GifReversed ?? "GIF reversed successfully!";
                            MessageBox.Show(
                                (SteamGifCropper.Properties.Resources.Message_GifReversedSuccess ?? "GIF reversed successfully!") + $"\n{outputFilePath}",
                                SteamGifCropper.Properties.Resources.Title_Success ?? "Success",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Information);
                        }
                    }
                    catch (Exception fallbackEx)
                    {
                        mainForm.lblStatus.Text = SteamGifCropper.Properties.Resources.Status_Error ?? "Error";
                        MessageBox.Show(
                            string.Format(SteamGifCropper.Properties.Resources.Error_GifReverseFailed ?? "Failed to reverse GIF: {0}", $"FFmpeg: {ex.Message}, ImageMagick: {fallbackEx.Message}"),
                            SteamGifCropper.Properties.Resources.Title_Error ?? "Error",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error);
                    }
                }
                finally
                {
                    mainForm.pBarTaskStatus.Visible = false;
                    mainForm.lblStatus.Text = SteamGifCropper.Properties.Resources.Status_Ready ?? "Ready";
                }
            }
        }

    }
}
