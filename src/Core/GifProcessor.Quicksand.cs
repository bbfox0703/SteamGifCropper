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
        #region Quicksand (流沙) Methods

        // Entry points: slice a 766px image / GIF into N horizontal bands, then wrap-scroll each band
        // horizontally at its own speed (a viscous shear gradient — bottom/top/middle fastest), easing
        // out to land back on the original image (seamless loop). Output is a single full-width 766px
        // GIF (no auto-split), chainable with other effects; split with the main "Split GIF" button.
        public static async Task QuicksandStaticImage(GifToolMainForm mainForm)
        {
            using var dialog = new QuicksandDialog(false);
            if (dialog.ShowDialog(mainForm) != DialogResult.OK)
            {
                return;
            }
            await RunQuicksand(mainForm, BuildQuicksandSettings(dialog, false));
        }

        public static async Task QuicksandGif(GifToolMainForm mainForm)
        {
            using var dialog = new QuicksandDialog(true);
            if (dialog.ShowDialog(mainForm) != DialogResult.OK)
            {
                return;
            }
            await RunQuicksand(mainForm, BuildQuicksandSettings(dialog, true));
        }

        private static QuicksandSettings BuildQuicksandSettings(QuicksandDialog dialog, bool isGif)
        {
            return new QuicksandSettings
            {
                InputFilePath = dialog.InputFilePath,
                OutputFilePath = dialog.OutputFilePath,
                IsGif = isGif,
                Layers = dialog.Layers,
                DurationSeconds = dialog.DurationSeconds,
                StartSeconds = dialog.StartSeconds,
                Fps = dialog.Fps,
                MaxRevolutions = dialog.MaxRevolutions,
                MinRevolutions = dialog.MinRevolutions,
                FastBand = dialog.FastBand,
                Viscosity = dialog.Viscosity,
                FlowRight = dialog.FlowRight,
                Axis = dialog.Axis,
                PlayGifDuringFlow = dialog.PlayGifDuringFlow,
                KeepOriginalSize = dialog.KeepOriginalSize,
            };
        }

        private static async Task RunQuicksand(GifToolMainForm mainForm, QuicksandSettings settings)
        {
            mainForm.Enabled = false;
            SetProgressRange(mainForm, 0, 100);
            SetProgressBar(mainForm.pBarTaskStatus, 0, 100);
            SetProgressVisible(mainForm, true);

            bool canceled = false;
            try
            {
                // Build the full-width flow animation (heavy → background thread). No auto-split: the
                // output is a single GIF so it can be chained with other effects and split later with the
                // main "Split GIF" button (which adds the 100px extension + 0x21 tail).
                await Task.Run(() =>
                {
                    SetStatusText(mainForm, SteamGifCropper.Properties.Resources.Status_QuicksandBuilding);
                    using var source = new MagickImageCollection(settings.InputFilePath);
                    source.Coalesce();

                    // Auto-resize to 766px wide when the input isn't already a supported width — unless the
                    // user opted to keep the original size (general-purpose use, not Steam prep).
                    uint width = source[0].Width;
                    if (!settings.KeepOriginalSize && !IsValidCanvasWidth(width))
                    {
                        foreach (var frame in source)
                        {
                            frame.ResetPage();
                            frame.Resize(SupportedWidth1, 0);
                        }
                        width = source[0].Width;
                    }

                    int canvasHeight = (int)source[0].Height;

                    // Warn before running at a large native size (output frame count: play-during keeps
                    // the GIF length; flow-then-play / static add the flow frames).
                    int outFrames = (settings.IsGif && settings.PlayGifDuringFlow)
                        ? source.Count
                        : (int)Math.Round(Math.Max(0.1, settings.DurationSeconds) * Math.Max(1, settings.Fps)) + (settings.IsGif ? source.Count : 0);
                    if (!ConfirmLargeCanvas(mainForm, Math.Max(source.Count, outFrames), width, (uint)canvasHeight))
                    {
                        canceled = true;
                        return;
                    }

                    using var animation = BuildQuicksandAnimation(mainForm, source, settings, (int)width, canvasHeight);
                    SetStatusText(mainForm, SteamGifCropper.Properties.Resources.Status_Saving);
                    animation.Optimize();
                    animation.Write(settings.OutputFilePath);
                });

                if (!canceled)
                {
                    SetProgressBar(mainForm.pBarTaskStatus, 100, 100);
                    SetStatusText(mainForm, SteamGifCropper.Properties.Resources.Status_Done);
                    WindowsThemeManager.ShowThemeAwareMessageBox(mainForm,
                        SteamGifCropper.Properties.Resources.Message_ProcessingComplete,
                        SteamGifCropper.Properties.Resources.Title_Success,
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
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

        // Builds the full-width (canvasWidth × canvasHeight) quicksand animation. Runs on a background
        // thread; all UI updates go through the marshaled SetProgressBar/SetStatusText helpers. Mirrors
        // the slot machine's two GIF playback models (see BuildSlotMachine*). Frame 0 is the original
        // image (all bands aligned) and every path returns to it.
        private static MagickImageCollection BuildQuicksandAnimation(GifToolMainForm mainForm,
            MagickImageCollection source, QuicksandSettings settings, int canvasWidth, int canvasHeight)
        {
            int layers = Math.Max(1, settings.Layers);

            int srcTicks = (int)source[0].AnimationTicksPerSecond;
            if (srcTicks <= 0) srcTicks = 100;

            // Horizontal flow slices the height into stacked rows; vertical slices the width into
            // side-by-side columns. bandTotal is the dimension the bands divide; the scroll wrap
            // dimension is the other one (handled per-axis in the build helpers).
            bool vertical = settings.Axis == QuicksandFlowAxis.Vertical;
            int bandTotal = vertical ? canvasWidth : canvasHeight;

            // Per-band bounds + whole-number revolution counts (the viscous shear gradient). Shared by
            // both playback models.
            var bands = new (int Start, int Size)[layers];
            int[] bandRevs = new int[layers];
            for (int b = 0; b < layers; b++)
            {
                bands[b] = QuicksandGeometry.BandBounds(b, layers, bandTotal);
                bandRevs[b] = QuicksandGeometry.BandRevolutions(b, layers, settings.MinRevolutions,
                    settings.MaxRevolutions, settings.FastBand, settings.Viscosity);
            }

            // GIF + "play during flow": the flow is mixed onto the LIVE frames for the first part, then
            // settles and the GIF keeps playing. Output length == GIF length (like slot machine "play
            // during spin").
            if (settings.IsGif && settings.PlayGifDuringFlow)
            {
                return BuildQuicksandPlayDuringFlow(mainForm, source, settings, bands, bandRevs,
                    canvasWidth, canvasHeight, srcTicks);
            }

            // Static image: flow over the single frame (loops, length == Duration). GIF + "flow, then
            // play": flow over a frozen frame 0, THEN the full GIF plays from frame 0 (output length ==
            // Duration + GIF length, like slot machine "spin, then play").
            return BuildQuicksandFlowThenPlay(mainForm, source, settings, bands, bandRevs,
                canvasWidth, canvasHeight, srcTicks);
        }

        // Crops band [start, start+size) from src along the flow-perpendicular axis (a full-width row
        // for horizontal flow, a full-height column for vertical), returning a page-reset clone.
        private static MagickImage CropQuicksandBand(IMagickImage<byte> src, bool vertical, int start, int size,
            int canvasWidth, int canvasHeight)
        {
            var band = (MagickImage)src.Clone();
            band.Crop(vertical
                ? new MagickGeometry(start, 0, (uint)size, (uint)canvasHeight)
                : new MagickGeometry(0, start, (uint)canvasWidth, (uint)size));
            band.ResetPage();
            return band;
        }

        // Wrap-scrolls a band along the flow axis by `off` then composites it back at its band position
        // and disposes it. Horizontal flow rolls/places on X; vertical on Y.
        private static void RollAndCompositeBand(MagickImage canvas, MagickImage band, bool vertical,
            int start, int off)
        {
            if (off != 0)
            {
                if (vertical) band.Roll(0, off);
                else band.Roll(off, 0);
            }
            if (vertical) canvas.Composite(band, start, 0, CompositeOperator.Over);
            else canvas.Composite(band, 0, start, CompositeOperator.Over);
            band.Dispose();
        }

        // GIF plays on its own timeline; the quicksand flow is mixed onto the LIVE frames for the first
        // `Duration` seconds, then settles (offset 0) and the GIF keeps playing untouched. Output length
        // == GIF length. The flow window is capped to the GIF length so it always returns to aligned
        // within the clip (seamless loop).
        private static MagickImageCollection BuildQuicksandPlayDuringFlow(GifToolMainForm mainForm,
            MagickImageCollection source, QuicksandSettings settings,
            (int Start, int Size)[] bands, int[] bandRevs, int canvasWidth, int canvasHeight, int srcTicks)
        {
            int layers = bands.Length;
            int n = source.Count;
            bool vertical = settings.Axis == QuicksandFlowAxis.Vertical;
            int wrapLength = vertical ? canvasHeight : canvasWidth;

            // Cumulative start time (seconds) of each source frame.
            double[] startSec = new double[n];
            double acc = 0.0;
            for (int i = 0; i < n; i++)
            {
                startSec[i] = acc;
                acc += (double)source[i].AnimationDelay / srcTicks;
            }
            double gifSeconds = acc;

            // Resolve the [start, duration] window (seconds, 2dp) into a clamped, frame-snapped frame
            // range. The flow eases in at startFrame and back to aligned at endFrame; outside the
            // window the frames pass through as the plain GIF.
            var (startFrame, endFrame) = GifEffectWindow.ResolveFrames(startSec, gifSeconds,
                settings.StartSeconds, settings.DurationSeconds);

            var result = new MagickImageCollection();
            int built = 0;
            for (int i = 0; i < n; i++)
            {
                double t = GifEffectWindow.FramePhase(i, startFrame, endFrame);

                var canvas = new MagickImage(MagickColors.Transparent, (uint)canvasWidth, (uint)canvasHeight);
                for (int b = 0; b < layers; b++)
                {
                    var band = CropQuicksandBand(source[i], vertical, bands[b].Start, bands[b].Size, canvasWidth, canvasHeight);
                    int off = QuicksandGeometry.BandOffset(t, bandRevs[b], wrapLength, settings.FlowRight);
                    RollAndCompositeBand(canvas, band, vertical, bands[b].Start, off);
                }

                canvas.AnimationDelay = source[i].AnimationDelay;
                canvas.AnimationTicksPerSecond = srcTicks;
                canvas.GifDisposeMethod = GifDisposeMethod.Background;
                result.Add(canvas);

                if (++built % 5 == 0 || built == n)
                {
                    SetProgressBar(mainForm.pBarTaskStatus, built * 100 / n, 100);
                    SetStatusText(mainForm, SteamGifCropper.Properties.Resources.Status_QuicksandBuilding);
                }
            }

            return result;
        }

        // Flow over a frozen first frame for `Duration` seconds at the chosen FPS. For a GIF this is
        // followed by the full source GIF played from frame 0 (output length == Duration + GIF length).
        // For a static image there is no play phase (output length == Duration; it just loops).
        private static MagickImageCollection BuildQuicksandFlowThenPlay(GifToolMainForm mainForm,
            MagickImageCollection source, QuicksandSettings settings,
            (int Start, int Size)[] bands, int[] bandRevs, int canvasWidth, int canvasHeight, int srcTicks)
        {
            int layers = bands.Length;
            int fps = Math.Max(1, settings.Fps);
            int delay = Math.Max(1, (int)Math.Round(100.0 / fps)); // GIF delay in 1/100 s
            int flowFrames = Math.Max(1, (int)Math.Round(settings.DurationSeconds * fps));
            int playFrames = settings.IsGif ? source.Count : 0;
            int totalFrames = flowFrames + playFrames;
            bool vertical = settings.Axis == QuicksandFlowAxis.Vertical;
            int wrapLength = vertical ? canvasHeight : canvasWidth;

            var result = new MagickImageCollection();
            var frozenBands = new MagickImage[layers];
            try
            {
                // Pre-crop each band from the frozen first frame once (they never change during the flow).
                for (int b = 0; b < layers; b++)
                {
                    frozenBands[b] = CropQuicksandBand(source[0], vertical, bands[b].Start, bands[b].Size, canvasWidth, canvasHeight);
                }

                int built = 0;

                // Phase 1: flow over the frozen frame 0.
                for (int f = 0; f < flowFrames; f++)
                {
                    double t = flowFrames <= 1 ? 0.0 : (double)f / flowFrames;
                    var canvas = new MagickImage(MagickColors.Transparent, (uint)canvasWidth, (uint)canvasHeight);
                    for (int b = 0; b < layers; b++)
                    {
                        var band = (MagickImage)frozenBands[b].Clone();
                        int off = QuicksandGeometry.BandOffset(t, bandRevs[b], wrapLength, settings.FlowRight);
                        RollAndCompositeBand(canvas, band, vertical, bands[b].Start, off);
                    }
                    canvas.AnimationDelay = (uint)delay;
                    canvas.AnimationTicksPerSecond = 100;
                    canvas.GifDisposeMethod = GifDisposeMethod.Background;
                    result.Add(canvas);

                    if (++built % 5 == 0 || built == totalFrames)
                    {
                        SetProgressBar(mainForm.pBarTaskStatus, built * 100 / totalFrames, 100);
                        SetStatusText(mainForm, SteamGifCropper.Properties.Resources.Status_QuicksandBuilding);
                    }
                }

                // Phase 2 (GIF only): play the full source GIF from frame 0 after the flow settles.
                if (settings.IsGif)
                {
                    foreach (var frame in source)
                    {
                        var play = (MagickImage)frame.Clone();
                        play.AnimationTicksPerSecond = srcTicks;
                        play.GifDisposeMethod = GifDisposeMethod.Background;
                        result.Add(play);

                        if (++built % 5 == 0 || built == totalFrames)
                        {
                            SetProgressBar(mainForm.pBarTaskStatus, built * 100 / totalFrames, 100);
                            SetStatusText(mainForm, SteamGifCropper.Properties.Resources.Status_QuicksandBuilding);
                        }
                    }
                }
            }
            finally
            {
                foreach (var fb in frozenBands)
                {
                    fb?.Dispose();
                }
            }

            return result;
        }

        #endregion

    }
}
