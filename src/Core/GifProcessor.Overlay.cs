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
        private static List<MagickImage> ResampleBaseFrames(MagickImageCollection baseCollection, MagickImageCollection overlayCollection)
        {
            var baseDelays = baseCollection.Select(f => (int)f.AnimationDelay).ToArray();
            int baseTotalDelay = baseDelays.Sum();
            var resampled = new List<MagickImage>(overlayCollection.Count);

            int overlayElapsed = 0;
            foreach (var overlayFrame in overlayCollection)
            {
                int startTime = baseTotalDelay == 0 ? 0 : overlayElapsed % baseTotalDelay;
                int cumulative = 0;
                int baseIndex = 0;
                for (int i = 0; i < baseDelays.Length; i++)
                {
                    cumulative += baseDelays[i];
                    if (startTime < cumulative)
                    {
                        baseIndex = i;
                        break;
                    }
                }

                resampled.Add((MagickImage)baseCollection[baseIndex].Clone());
                overlayElapsed += (int)overlayFrame.AnimationDelay;
            }

            return resampled;
        }

        private static Task ProcessStaticOverlay(GifToolMainForm mainForm, MagickImageCollection baseCollection,
            MagickImageCollection overlayCollection, MagickImageCollection resultCollection,
            int offsetX, int offsetY, bool resampleBase, int baseWidth, int baseHeight)
        {
            // Initialize progress bar
            if (mainForm != null)
            {
                SetProgressRange(mainForm, 0, 100);
                SetProgressBar(mainForm.pBarTaskStatus, 0, 100);
            }

            if (resampleBase)
            {
                var resampledBaseFrames = ResampleBaseFrames(baseCollection, overlayCollection);
                int overlayCount = overlayCollection.Count;

                SetStatusText(mainForm, $"Processing static overlay (resampled): 0/{overlayCount} frames");

                for (int i = 0; i < overlayCount; i++)
                {
                    using var baseFrame = resampledBaseFrames[i];
                    using var overlayFrame = overlayCollection[i].Clone();

                    // Handle partial visibility and bounds checking
                    var overlayGeometry = CalculateOverlayGeometry((MagickImage)overlayFrame, baseWidth, baseHeight, offsetX, offsetY);
                    if (overlayGeometry.Width <= 0 || overlayGeometry.Height <= 0)
                    {
                        // Overlay is completely out of bounds, add base frame only
                        resultCollection.Add(baseFrame.Clone());
                        continue;
                    }

                    // Crop overlay if it extends beyond base boundaries
                    if (overlayGeometry.CropRequired)
                    {
                        overlayFrame.Crop(new MagickGeometry(overlayGeometry.CropX, overlayGeometry.CropY,
                            (uint)overlayGeometry.Width, (uint)overlayGeometry.Height));
                        overlayFrame.Page = new MagickGeometry(0, 0, overlayFrame.Width, overlayFrame.Height);
                    }

                    baseFrame.Composite(overlayFrame, overlayGeometry.CompositeX, overlayGeometry.CompositeY, CompositeOperator.Over);
                    baseFrame.AnimationDelay = overlayFrame.AnimationDelay;
                    baseFrame.AnimationTicksPerSecond = overlayFrame.AnimationTicksPerSecond;
                    baseFrame.GifDisposeMethod = GifDisposeMethod.Background;

                    resultCollection.Add(baseFrame.Clone());
                    UpdateFrameProgress(mainForm, i + 1, overlayCount);
                }

                resampledBaseFrames.Clear();
            }
            else
            {
                int baseCount = baseCollection.Count;
                var overlayDelays = overlayCollection.Select(f => (int)f.AnimationDelay).ToArray();

                SetStatusText(mainForm, $"Processing static overlay: 0/{baseCount} frames");
                int overlayTotalDelay = overlayDelays.Sum();
                int baseElapsed = 0;

                for (int i = 0; i < baseCount; i++)
                {
                    using var baseFrame = (MagickImage)baseCollection[i].Clone();

                    int startTime = overlayTotalDelay == 0 ? 0 : baseElapsed % overlayTotalDelay;
                    int cumulative = 0;
                    int overlayIndex = 0;
                    for (int j = 0; j < overlayDelays.Length; j++)
                    {
                        cumulative += overlayDelays[j];
                        if (startTime < cumulative)
                        {
                            overlayIndex = j;
                            break;
                        }
                    }

                    using var overlayFrame = overlayCollection[overlayIndex].Clone();

                    // Handle partial visibility and bounds checking
                    var overlayGeometry = CalculateOverlayGeometry((MagickImage)overlayFrame, baseWidth, baseHeight, offsetX, offsetY);
                    if (overlayGeometry.Width <= 0 || overlayGeometry.Height <= 0)
                    {
                        // Overlay is completely out of bounds, add base frame only
                        baseElapsed += (int)baseCollection[i].AnimationDelay;
                        resultCollection.Add(baseFrame.Clone());
                        continue;
                    }

                    // Crop overlay if it extends beyond base boundaries
                    if (overlayGeometry.CropRequired)
                    {
                        overlayFrame.Crop(new MagickGeometry(overlayGeometry.CropX, overlayGeometry.CropY,
                            (uint)overlayGeometry.Width, (uint)overlayGeometry.Height));
                        overlayFrame.Page = new MagickGeometry(0, 0, overlayFrame.Width, overlayFrame.Height);
                    }

                    baseFrame.Composite(overlayFrame, overlayGeometry.CompositeX, overlayGeometry.CompositeY, CompositeOperator.Over);
                    baseFrame.GifDisposeMethod = GifDisposeMethod.Background;

                    resultCollection.Add(baseFrame.Clone());

                    baseElapsed += (int)baseCollection[i].AnimationDelay;
                    UpdateFrameProgress(mainForm, i + 1, baseCount);
                }
            }
            return Task.CompletedTask;
        }

        private static (int signX, int signY) GetDirectionSigns(ScrollDirection direction)
        {
            int signX = 0, signY = 0;
            switch (direction)
            {
                case ScrollDirection.Right: signX = 1; break;
                case ScrollDirection.Left: signX = -1; break;
                case ScrollDirection.Down: signY = 1; break;
                case ScrollDirection.Up: signY = -1; break;
                case ScrollDirection.LeftUp: signX = -1; signY = -1; break;
                case ScrollDirection.LeftDown: signX = -1; signY = 1; break;
                case ScrollDirection.RightUp: signX = 1; signY = -1; break;
                case ScrollDirection.RightDown: signX = 1; signY = 1; break;
            }
            return (signX, signY);
        }

        private static Task ProcessMovingOverlay(GifToolMainForm mainForm, MagickImageCollection baseCollection,
            MagickImageCollection overlayCollection, MagickImageCollection resultCollection,
            ScrollDirection direction, int stepPixels, int moveCount, bool infiniteMovement,
            bool resampleBase, int baseWidth, int baseHeight, int overlayWidth, int overlayHeight,
            int startX, int startY)
        {
            // Calculate movement direction vectors
            (int signX, int signY) = GetDirectionSigns(direction);

            // Calculate movement parameters
            int totalFrames;
            if (infiniteMovement)
            {
                // Match base GIF duration
                totalFrames = baseCollection.Count;
            }
            else
            {
                // Use specified move count
                totalFrames = moveCount;
            }

            // Starting position is provided from Static Overlay Position coordinates
            // Movement will begin from these coordinates and proceed in the specified direction

            // Initialize progress bar
            if (mainForm != null)
            {
                SetProgressRange(mainForm, 0, 100);
                SetProgressBar(mainForm.pBarTaskStatus, 0, 100);
            }

            SetStatusText(mainForm, $"Processing moving overlay: 0/{totalFrames} frames");

            for (int frame = 0; frame < totalFrames; frame++)
            {
                // Calculate current overlay position
                int currentX = startX + (signX * stepPixels * frame);
                int currentY = startY + (signY * stepPixels * frame);

                // Get corresponding base and overlay frames
                var baseFrame = baseCollection[frame % baseCollection.Count].Clone();
                var overlayFrame = overlayCollection[frame % overlayCollection.Count].Clone();

                // Handle partial visibility and bounds checking
                var overlayGeometry = CalculateOverlayGeometry((MagickImage)overlayFrame, baseWidth, baseHeight, currentX, currentY);

                if (overlayGeometry.Width > 0 && overlayGeometry.Height > 0)
                {
                    // Overlay is at least partially visible
                    if (overlayGeometry.CropRequired)
                    {
                        overlayFrame.Crop(new MagickGeometry(overlayGeometry.CropX, overlayGeometry.CropY,
                            (uint)overlayGeometry.Width, (uint)overlayGeometry.Height));
                        overlayFrame.Page = new MagickGeometry(0, 0, overlayFrame.Width, overlayFrame.Height);
                    }

                    baseFrame.Composite(overlayFrame, overlayGeometry.CompositeX, overlayGeometry.CompositeY, CompositeOperator.Over);
                }

                baseFrame.GifDisposeMethod = GifDisposeMethod.Background;
                resultCollection.Add(baseFrame);

                overlayFrame.Dispose();
                UpdateFrameProgress(mainForm, frame + 1, totalFrames);
            }
            return Task.CompletedTask;
        }

        private struct OverlayGeometry
        {
            public int Width;
            public int Height;
            public int CompositeX;
            public int CompositeY;
            public int CropX;
            public int CropY;
            public bool CropRequired;
        }

        private static OverlayGeometry CalculateOverlayGeometry(MagickImage overlayFrame, int baseWidth, int baseHeight, int offsetX, int offsetY)
        {
            var geometry = new OverlayGeometry();
            int overlayWidth = (int)overlayFrame.Width;
            int overlayHeight = (int)overlayFrame.Height;

            // Calculate intersection with base boundaries
            int leftBound = Math.Max(0, offsetX);
            int topBound = Math.Max(0, offsetY);
            int rightBound = Math.Min(baseWidth, offsetX + overlayWidth);
            int bottomBound = Math.Min(baseHeight, offsetY + overlayHeight);

            geometry.Width = Math.Max(0, rightBound - leftBound);
            geometry.Height = Math.Max(0, bottomBound - topBound);

            if (geometry.Width <= 0 || geometry.Height <= 0)
            {
                // Completely out of bounds
                return geometry;
            }

            // Determine if cropping is needed
            geometry.CropRequired = (offsetX < 0 || offsetY < 0 || offsetX + overlayWidth > baseWidth || offsetY + overlayHeight > baseHeight);

            if (geometry.CropRequired)
            {
                // Calculate crop coordinates within overlay image
                geometry.CropX = Math.Max(0, -offsetX);
                geometry.CropY = Math.Max(0, -offsetY);
            }

            // Calculate composite position (always >= 0)
            geometry.CompositeX = Math.Max(0, offsetX);
            geometry.CompositeY = Math.Max(0, offsetY);

            return geometry;
        }

        public static async Task OverlayGif(GifToolMainForm mainForm)
        {
            using var dialog = new OverlayGifDialog();
            if (dialog.ShowDialog(mainForm) != DialogResult.OK)
                return;

            string basePath = dialog.BaseGifPath;
            string overlayPath = dialog.OverlayGifPath;
            string outputPath = dialog.OutputGifPath;
            ImageInputValidator.ValidateGif(basePath);
            ImageInputValidator.ValidateGif(overlayPath);
            bool resampleBase = dialog.ResampleBaseFrames;

            // Capture every dialog value on the UI thread; the dialog is a control and must not be
            // touched from the background worker.
            bool useStaticOverlay = dialog.UseStaticOverlay;
            int staticOverlayX = dialog.StaticOverlayX;
            int staticOverlayY = dialog.StaticOverlayY;
            var movementDirection = dialog.MovementDirection;
            int stepPixels = dialog.StepPixels;
            int moveCount = dialog.MoveCount;
            bool infiniteMovement = dialog.InfiniteMovement;

            mainForm.Enabled = false;
            SetProgressRange(mainForm, 0, 100);
            SetProgressBar(mainForm.pBarTaskStatus, 0, 100);
            try
            {
                SetStatusText(mainForm, SteamGifCropper.Properties.Resources.Status_Loading);

                await Task.Run(async () =>
                {
                    using var baseCollection = new MagickImageCollection(basePath);
                    using var overlayCollection = new MagickImageCollection(overlayPath);
                    using var resultCollection = new MagickImageCollection();

                    int baseWidth = (int)baseCollection[0].Width;
                    int baseHeight = (int)baseCollection[0].Height;
                    int overlayWidth = (int)overlayCollection[0].Width;
                    int overlayHeight = (int)overlayCollection[0].Height;

                    baseCollection.Coalesce();
                    overlayCollection.Coalesce();

                    if (useStaticOverlay)
                    {
                        // Static overlay - use original logic with fixed position
                        await ProcessStaticOverlay(mainForm, baseCollection, overlayCollection, resultCollection,
                            staticOverlayX, staticOverlayY, resampleBase, baseWidth, baseHeight);
                    }
                    else
                    {
                        // Moving overlay - new logic starting from static overlay position
                        await ProcessMovingOverlay(mainForm, baseCollection, overlayCollection, resultCollection,
                            movementDirection, stepPixels, moveCount, infiniteMovement,
                            resampleBase, baseWidth, baseHeight, overlayWidth, overlayHeight,
                            staticOverlayX, staticOverlayY);
                    }

                    resultCollection.Quantize();
                    resultCollection.Optimize();

                    SetStatusText(mainForm, SteamGifCropper.Properties.Resources.Status_Saving);
                    resultCollection.Write(outputPath);
                });

                SetStatusText(mainForm, SteamGifCropper.Properties.Resources.Status_Done);
                WindowsThemeManager.ShowThemeAwareMessageBox(mainForm,
                    SteamGifCropper.Properties.Resources.Message_OverlayComplete,
                    SteamGifCropper.Properties.Resources.Title_Success,
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                SetStatusText(mainForm, SteamGifCropper.Properties.Resources.Status_Error);
                WindowsThemeManager.ShowThemeAwareMessageBox(mainForm,
                    $"Error: {ex.Message}",
                    SteamGifCropper.Properties.Resources.Title_Error,
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
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
