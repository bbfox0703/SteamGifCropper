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
        #region Slot Machine (拉霸) Methods

        // 766px slot-machine reveal for a STATIC image: each of the 5 Steam columns becomes a vertical
        // reel that wrap-scrolls its own slice, decelerates and locks left-to-right onto the image,
        // then the result is split into the 5 Steam parts.
        public static async Task SlotMachineStaticImage(GifToolMainForm mainForm)
        {
            using var dialog = new SlotMachineDialog(false);
            if (dialog.ShowDialog(mainForm) != DialogResult.OK)
                return;

            ImageInputValidator.ValidateImage(dialog.InputFilePath);
            await RunSlotMachine(mainForm, BuildSettings(dialog, false));
        }

        // Same slot-machine reveal but for an animated GIF: after the reels lock, the GIF plays through.
        public static async Task SlotMachineGif(GifToolMainForm mainForm)
        {
            using var dialog = new SlotMachineDialog(true);
            if (dialog.ShowDialog(mainForm) != DialogResult.OK)
                return;

            ImageInputValidator.ValidateGif(dialog.InputFilePath);
            await RunSlotMachine(mainForm, BuildSettings(dialog, true));
        }

        private static SlotMachineSettings BuildSettings(SlotMachineDialog dialog, bool isGif)
        {
            return new SlotMachineSettings
            {
                InputFilePath = dialog.InputFilePath,
                OutputFilePath = dialog.OutputFilePath,
                IsGif = isGif,
                DurationSeconds = dialog.DurationSeconds,
                StartSeconds = dialog.StartSeconds,
                DurationVariancePercent = dialog.DurationVariancePercent,
                Fps = dialog.Fps,
                Spins = dialog.Spins,
                SpinsVariancePercent = dialog.SpinsVariancePercent,
                BounceSeconds = dialog.BounceSeconds,
                TopToBottom = dialog.TopToBottom,
                HoldSeconds = dialog.HoldSeconds,
                PlayGifDuringSpin = dialog.PlayGifDuringSpin
            };
        }

        private static async Task RunSlotMachine(GifToolMainForm mainForm, SlotMachineSettings settings)
        {
            mainForm.Enabled = false;
            SetProgressRange(mainForm, 0, 100);
            SetProgressBar(mainForm.pBarTaskStatus, 0, 100);
            SetProgressVisible(mainForm, true);

            try
            {
                // Build the full-width 766px slot-machine animation (heavy → background thread).
                // No auto-split: the output is a single 766px GIF so it can be chained with other
                // effects (grid mosaic, scroll, ...) and split with the main "Split GIF" button when
                // ready (that applies the 100px extension + 0x21 tail byte per part).
                await Task.Run(() =>
                {
                    SetStatusText(mainForm, SteamGifCropper.Properties.Resources.Status_SlotMachineBuilding);
                    using var source = new MagickImageCollection(settings.InputFilePath);
                    source.Coalesce();

                    // Auto-resize to 766px wide when the input isn't already a supported width.
                    uint width = source[0].Width;
                    if (!IsValidCanvasWidth(width))
                    {
                        foreach (var frame in source)
                        {
                            frame.ResetPage();
                            frame.Resize(SupportedWidth1, 0);
                        }
                        width = source[0].Width;
                    }

                    var ranges = GetCropRanges(width);
                    int canvasHeight = (int)source[0].Height;

                    using var animation = BuildSlotMachineAnimation(mainForm, source, settings, ranges, (int)width, canvasHeight);
                    SetStatusText(mainForm, SteamGifCropper.Properties.Resources.Status_Saving);
                    animation.Optimize();
                    animation.Write(settings.OutputFilePath);
                });

                SetProgressBar(mainForm.pBarTaskStatus, 100, 100);
                SetStatusText(mainForm, SteamGifCropper.Properties.Resources.Status_Done);
                WindowsThemeManager.ShowThemeAwareMessageBox(mainForm,
                    SteamGifCropper.Properties.Resources.Message_ProcessingComplete,
                    SteamGifCropper.Properties.Resources.Title_Success,
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                SetStatusText(mainForm, SteamGifCropper.Properties.Resources.Status_Error);
                WindowsThemeManager.ShowThemeAwareMessageBox(mainForm,
                    string.Format(SteamGifCropper.Properties.Resources.Error_Occurred, ex.Message),
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

        // Builds the full-width (canvasWidth × canvasHeight) spinning animation. Runs on a background
        // thread; all UI updates go through the marshaled SetProgressBar/SetStatusText helpers.
        private static MagickImageCollection BuildSlotMachineAnimation(GifToolMainForm mainForm,
            MagickImageCollection source, SlotMachineSettings settings,
            (int Start, int End)[] ranges, int canvasWidth, int canvasHeight)
        {
            int reelCount = ranges.Length;
            int spins = Math.Max(1, settings.Spins);

            int srcTicks = (int)source[0].AnimationTicksPerSecond;
            if (srcTicks <= 0) srcTicks = 100;

            // GIF length in seconds (caps spin time and drives the play-during-spin model).
            double gifSeconds = 0.0;
            if (settings.IsGif)
            {
                foreach (var frame in source)
                {
                    gifSeconds += (double)frame.AnimationDelay / srcTicks;
                }
            }
            bool playDuring = settings.IsGif && settings.PlayGifDuringSpin;

            // "Play during spin": clamp the [start, duration] window (2dp) into the GIF and spin within
            // it (reels start at the window, lock before the GIF ends). "Spin then play"/static ignore
            // the start offset (the spin runs over a frozen/static frame, then the GIF plays).
            double windowStartSec = 0.0;
            double baseDuration = settings.DurationSeconds;
            double maxSpinSeconds;
            if (playDuring)
            {
                var (cs, cd) = GifEffectWindow.Clamp(settings.StartSeconds, settings.DurationSeconds, gifSeconds);
                windowStartSec = cs;
                baseDuration = cd;
                maxSpinSeconds = gifSeconds - windowStartSec; // a reel must lock before the GIF ends
            }
            else
            {
                maxSpinSeconds = (settings.IsGif && gifSeconds > 0.0) ? gifSeconds : double.MaxValue;
            }

            // Randomize each reel's stop time (seconds) and revolution count so which reel stops first
            // (and which spins longest) is non-deterministic.
            var rng = new Random();
            double[] reelStopSec = new double[reelCount];
            int[] reelSpins = new int[reelCount];
            for (int c = 0; c < reelCount; c++)
            {
                double durSec = SlotMachineGeometry.ApplyVariance(baseDuration, settings.DurationVariancePercent, rng.NextDouble());
                if (durSec > maxSpinSeconds) durSec = maxSpinSeconds;
                if (durSec < 0.1) durSec = 0.1;
                reelStopSec[c] = durSec;

                double spinVal = SlotMachineGeometry.ApplyVariance(spins, settings.SpinsVariancePercent, rng.NextDouble());
                reelSpins[c] = Math.Max(1, (int)Math.Round(spinVal));
            }

            if (playDuring)
            {
                return BuildSlotMachinePlayDuringSpin(mainForm, source, settings, ranges, canvasWidth, canvasHeight, reelStopSec, reelSpins, srcTicks, gifSeconds, windowStartSec);
            }

            return BuildSlotMachineSpinThenLock(mainForm, source, settings, ranges, canvasWidth, canvasHeight, reelStopSec, reelSpins, srcTicks);
        }

        // Reels spin over a frozen first frame (or the static image) and lock at their randomized times,
        // then either hold the result (static) or play the full GIF (gif "spin, then play").
        private static MagickImageCollection BuildSlotMachineSpinThenLock(GifToolMainForm mainForm,
            MagickImageCollection source, SlotMachineSettings settings,
            (int Start, int End)[] ranges, int canvasWidth, int canvasHeight,
            double[] reelStopSec, int[] reelSpins, int srcTicks)
        {
            int reelCount = ranges.Length;
            int fps = Math.Max(1, settings.Fps);
            int delay = Math.Max(1, (int)Math.Round(100.0 / fps)); // GIF delay in 1/100 s
            int overshootFrames = Math.Max(0, (int)Math.Round(settings.BounceSeconds * fps));

            int[] reelStop = new int[reelCount];
            int maxStop = 1;
            for (int c = 0; c < reelCount; c++)
            {
                reelStop[c] = Math.Max(1, (int)Math.Round(reelStopSec[c] * fps));
                if (reelStop[c] > maxStop) maxStop = reelStop[c];
            }

            int totalSpinFrames = maxStop + overshootFrames;
            int endFrames = settings.IsGif ? source.Count : Math.Max(1, settings.HoldSeconds * fps);
            int totalFrames = totalSpinFrames + endFrames;
            int built = 0;

            var result = new MagickImageCollection();
            var slices = new MagickImage[reelCount];
            try
            {
                // Pre-crop each column slice from the first (locked/"prize") frame.
                for (int c = 0; c < reelCount; c++)
                {
                    int w = ranges[c].End - ranges[c].Start + 1;
                    var slice = (MagickImage)source[0].Clone();
                    slice.Crop(new MagickGeometry(ranges[c].Start, 0, (uint)w, (uint)canvasHeight));
                    slice.ResetPage();
                    slices[c] = slice;
                }

                // Spin phase: each reel wrap-scrolls, decelerates to its own randomized lock, then bounces.
                for (int t = 0; t < totalSpinFrames; t++)
                {
                    var canvas = new MagickImage(MagickColors.Transparent, (uint)canvasWidth, (uint)canvasHeight);
                    for (int c = 0; c < reelCount; c++)
                    {
                        int off = SlotMachineGeometry.ReelOffsetY(t, reelStop[c], reelSpins[c], canvasHeight, settings.TopToBottom, overshootFrames);
                        using var tmp = (MagickImage)slices[c].Clone();
                        if (off != 0)
                        {
                            tmp.Roll(0, off);
                        }
                        canvas.Composite(tmp, ranges[c].Start, 0, CompositeOperator.Over);
                    }
                    canvas.AnimationDelay = (uint)delay;
                    canvas.AnimationTicksPerSecond = 100;
                    canvas.GifDisposeMethod = GifDisposeMethod.Background;
                    result.Add(canvas);

                    if (++built % 5 == 0 || built == totalFrames)
                    {
                        SetProgressBar(mainForm.pBarTaskStatus, built * 100 / totalFrames, 100);
                        SetStatusText(mainForm, SteamGifCropper.Properties.Resources.Status_SlotMachineBuilding);
                    }
                }

                // End phase.
                if (settings.IsGif)
                {
                    // Play the original GIF through once after the reels lock.
                    foreach (var frame in source)
                    {
                        var play = (MagickImage)frame.Clone();
                        play.AnimationTicksPerSecond = srcTicks;
                        play.GifDisposeMethod = GifDisposeMethod.Background;
                        result.Add(play);
                        if (++built % 5 == 0 || built == totalFrames)
                        {
                            SetProgressBar(mainForm.pBarTaskStatus, built * 100 / totalFrames, 100);
                        }
                    }
                }
                else
                {
                    // Hold the locked image for a moment before the loop restarts.
                    for (int h = 0; h < endFrames; h++)
                    {
                        var hold = (MagickImage)source[0].Clone();
                        hold.AnimationDelay = (uint)delay;
                        hold.AnimationTicksPerSecond = 100;
                        hold.GifDisposeMethod = GifDisposeMethod.Background;
                        result.Add(hold);
                        if (++built % 5 == 0 || built == totalFrames)
                        {
                            SetProgressBar(mainForm.pBarTaskStatus, built * 100 / totalFrames, 100);
                        }
                    }
                }
            }
            finally
            {
                foreach (var slice in slices)
                {
                    slice?.Dispose();
                }
            }

            return result;
        }

        // GIF plays on its own timeline; the reels spin over the LIVE frames for the first part, then
        // lock and the GIF keeps playing. Output length == GIF length (the spin consumes its first part).
        private static MagickImageCollection BuildSlotMachinePlayDuringSpin(GifToolMainForm mainForm,
            MagickImageCollection source, SlotMachineSettings settings,
            (int Start, int End)[] ranges, int canvasWidth, int canvasHeight,
            double[] reelStopSec, int[] reelSpins, int srcTicks, double gifSeconds, double windowStartSec)
        {
            int reelCount = ranges.Length;
            int n = source.Count;

            // Cumulative start time (seconds) of each GIF frame.
            double[] startSec = new double[n];
            double acc = 0.0;
            for (int i = 0; i < n; i++)
            {
                startSec[i] = acc;
                acc += (double)source[i].AnimationDelay / srcTicks;
            }
            double gifFps = (gifSeconds > 0.0) ? n / gifSeconds : n;
            int overshootGif = Math.Max(0, (int)Math.Round(settings.BounceSeconds * gifFps));

            // The spin begins at the (frame-snapped) window start; reels lock at window start + their own
            // randomized duration. Before startFrame the reels show the plain live GIF (offset 0).
            int startFrame = GifEffectWindow.NearestFrameIndex(startSec, windowStartSec);
            int[] reelStopFrame = new int[reelCount];
            for (int c = 0; c < reelCount; c++)
            {
                reelStopFrame[c] = Math.Max(startFrame + 1,
                    GifEffectWindow.NearestFrameIndex(startSec, windowStartSec + reelStopSec[c]));
            }

            var result = new MagickImageCollection();
            int built = 0;
            for (int i = 0; i < n; i++)
            {
                var canvas = new MagickImage(MagickColors.Transparent, (uint)canvasWidth, (uint)canvasHeight);
                for (int c = 0; c < reelCount; c++)
                {
                    int w = ranges[c].End - ranges[c].Start + 1;
                    using var col = (MagickImage)source[i].Clone();
                    col.Crop(new MagickGeometry(ranges[c].Start, 0, (uint)w, (uint)canvasHeight));
                    col.ResetPage();
                    int local = i - startFrame;
                    int off = local <= 0
                        ? 0
                        : SlotMachineGeometry.ReelOffsetY(local, reelStopFrame[c] - startFrame, reelSpins[c], canvasHeight, settings.TopToBottom, overshootGif);
                    if (off != 0)
                    {
                        col.Roll(0, off);
                    }
                    canvas.Composite(col, ranges[c].Start, 0, CompositeOperator.Over);
                }
                canvas.AnimationDelay = source[i].AnimationDelay;
                canvas.AnimationTicksPerSecond = srcTicks;
                canvas.GifDisposeMethod = GifDisposeMethod.Background;
                result.Add(canvas);

                if (++built % 5 == 0 || built == n)
                {
                    SetProgressBar(mainForm.pBarTaskStatus, built * 100 / n, 100);
                    SetStatusText(mainForm, SteamGifCropper.Properties.Resources.Status_SlotMachineBuilding);
                }
            }

            return result;
        }

        #endregion

    }
}
