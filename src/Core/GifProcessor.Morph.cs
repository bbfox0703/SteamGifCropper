using ImageMagick;
using SteamGifCropper;
using SteamGifCropper.Properties;
using System;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GifProcessorApp
{
    public static partial class GifProcessor
    {
        #region A->B Morph Transition (疊圖轉換) Methods

        // Morph one clip into another: A plays for PreRoll seconds, then A morphs into B over MorphSeconds
        // (A is fully gone by the end and its leftover is discarded), then B's remaining footage plays to
        // the end. Two morph styles: raindrop-spread reveal and tile-flip. Output is a single full-width
        // 766px GIF (no auto-split), chainable; split with the main "Split GIF" button.
        public static async Task MorphTransition(GifToolMainForm mainForm)
        {
            using var dialog = new MorphTransitionDialog();
            if (dialog.ShowDialog(mainForm) != DialogResult.OK)
            {
                return;
            }
            await RunMorph(mainForm, dialog.BuildSettings());
        }

        private static async Task RunMorph(GifToolMainForm mainForm, MorphSettings settings)
        {
            mainForm.Enabled = false;
            SetProgressRange(mainForm, 0, 100);
            SetProgressBar(mainForm.pBarTaskStatus, 0, 100);
            SetProgressVisible(mainForm, true);

            bool canceled = false;
            try
            {
                await Task.Run(() =>
                {
                    SetStatusText(mainForm, SteamGifCropper.Properties.Resources.Status_MorphBuilding);

                    using var aSrc = new MagickImageCollection(settings.InputAPath);
                    aSrc.Coalesce();
                    using var bSrc = new MagickImageCollection(settings.InputBPath);
                    bSrc.Coalesce();

                    // Target canvas: A's size, or fit A to 766px wide when its width isn't a supported one.
                    uint aw = aSrc[0].Width;
                    uint ah = aSrc[0].Height;
                    if (!settings.KeepOriginalSize && !IsValidCanvasWidth(aw))
                    {
                        foreach (var frame in aSrc)
                        {
                            frame.ResetPage();
                            frame.Resize(SupportedWidth1, 0);
                        }
                        aw = aSrc[0].Width;
                        ah = aSrc[0].Height;
                    }
                    int targetW = (int)aw;
                    int targetH = (int)ah;

                    // Fit B onto the same canvas (preserve aspect, centre) so the morph blends pixel-for-pixel.
                    using var bFit = new MagickImageCollection();
                    foreach (var frame in bSrc)
                    {
                        bFit.Add(FitToCanvas(frame, targetW, targetH));
                    }

                    // Per-frame start times + total length for A and B.
                    int aTicks = (int)aSrc[0].AnimationTicksPerSecond; if (aTicks <= 0) aTicks = 100;
                    int bTicks = (int)bSrc[0].AnimationTicksPerSecond; if (bTicks <= 0) bTicks = 100;
                    double[] aStart = FrameStartSeconds(aSrc, aTicks, out double aDur);
                    double[] bStart = FrameStartSeconds(bSrc, bTicks, out double bDur);

                    double morph = MorphTimeline.ClampMorph(settings.MorphSeconds, bDur);
                    double preRoll = settings.PreRollSeconds < 0.0 ? 0.0 : settings.PreRollSeconds;

                    int estFrames = EstimateMorphFrames(aStart, bStart, preRoll, morph, settings.Fps);
                    if (!ConfirmLargeCanvas(mainForm, estFrames, (uint)targetW, (uint)targetH))
                    {
                        canceled = true;
                        return;
                    }

                    using var animation = BuildMorph(mainForm, aSrc, bFit, aStart, bStart, aDur, bDur,
                        aTicks, bTicks, targetW, targetH, preRoll, morph, settings, estFrames);
                    SetStatusText(mainForm, SteamGifCropper.Properties.Resources.Status_Saving);
                    animation.Optimize();
                    animation.Write(settings.OutputPath);
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

        // Cumulative native start time (seconds) of each frame; out total clip length.
        private static double[] FrameStartSeconds(MagickImageCollection col, int ticks, out double totalSeconds)
        {
            int n = col.Count;
            double[] startSec = new double[n];
            double acc = 0.0;
            for (int i = 0; i < n; i++)
            {
                startSec[i] = acc;
                acc += (double)col[i].AnimationDelay / ticks;
            }
            totalSeconds = acc;
            return startSec;
        }

        // preRoll frames of A + N morph frames + remaining frames of B.
        private static int EstimateMorphFrames(double[] aStart, double[] bStart, double preRoll, double morph, int fps)
        {
            int pre = 0;
            for (int i = 0; i < aStart.Length; i++) if (aStart[i] < preRoll) pre++;
            int n = Math.Max(1, (int)Math.Round(morph * Math.Max(1, fps)));
            int rem = 0;
            for (int i = 0; i < bStart.Length; i++) if (bStart[i] >= morph) rem++;
            return pre + n + rem;
        }

        // Scale `src` to fit a targetW x targetH canvas (preserve aspect), centred on transparent, with the
        // source frame's timing carried over. Returns a new image exactly targetW x targetH.
        private static MagickImage FitToCanvas(IMagickImage<byte> src, int targetW, int targetH)
        {
            var scaled = (MagickImage)src.Clone();
            scaled.ResetPage();
            if ((int)scaled.Width != targetW || (int)scaled.Height != targetH)
            {
                scaled.Resize(new MagickGeometry((uint)targetW, (uint)targetH) { IgnoreAspectRatio = false });
            }
            if ((int)scaled.Width == targetW && (int)scaled.Height == targetH)
            {
                scaled.AnimationDelay = src.AnimationDelay;
                scaled.AnimationTicksPerSecond = src.AnimationTicksPerSecond;
                return scaled;
            }

            var canvas = new MagickImage(MagickColors.Transparent, (uint)targetW, (uint)targetH);
            int x = (targetW - (int)scaled.Width) / 2;
            int y = (targetH - (int)scaled.Height) / 2;
            canvas.Composite(scaled, x, y, CompositeOperator.Over);
            canvas.ResetPage();
            canvas.AnimationDelay = src.AnimationDelay;
            canvas.AnimationTicksPerSecond = src.AnimationTicksPerSecond;
            scaled.Dispose();
            return canvas;
        }

        private static MagickImageCollection BuildMorph(GifToolMainForm mainForm,
            MagickImageCollection aSrc, MagickImageCollection bFit,
            double[] aStart, double[] bStart, double aDur, double bDur, int aTicks, int bTicks,
            int targetW, int targetH, double preRoll, double morph, MorphSettings settings, int totalFrames)
        {
            int fps = Math.Max(1, settings.Fps);
            int delay = Math.Max(1, (int)Math.Round(100.0 / fps));

            RaindropSeed[] drops = settings.Style == MorphStyle.RaindropReveal
                ? RaindropRevealField.BuildDrops(settings, targetW, targetH)
                : null;
            TileGrid grid = (settings.Style == MorphStyle.TileFlip || settings.Style == MorphStyle.Jigsaw)
                ? TileFlipGeometry.ComputeGrid(targetW, targetH, settings.Divisions)
                : default;
            SpotlightParams spot = settings.Style == MorphStyle.Spotlight
                ? settings.ToSpotlightParams()
                : default;
            BrickParams brick = settings.Style == MorphStyle.Brick
                ? settings.ToBrickParams()
                : default;
            WaterParams water = settings.Style == MorphStyle.WaterFill
                ? settings.ToWaterParams()
                : default;

            var result = new MagickImageCollection();
            int built = 0;

            // Phase 1: A alone for the pre-roll, at A's native timing.
            for (int i = 0; i < aSrc.Count; i++)
            {
                if (aStart[i] >= preRoll) break;
                var frame = (MagickImage)aSrc[i].Clone();
                frame.ResetPage();
                frame.AnimationDelay = aSrc[i].AnimationDelay;
                frame.AnimationTicksPerSecond = aTicks;
                frame.GifDisposeMethod = GifDisposeMethod.Background;
                result.Add(frame);
                ReportMorph(mainForm, ++built, totalFrames);
            }

            // Phase 2: the morph window (A and B both advance; A is fully gone by t=1).
            int n = Math.Max(1, (int)Math.Round(morph * fps));
            for (int k = 0; k < n; k++)
            {
                double tA = preRoll + (double)k / fps;
                int idxA = GifEffectWindow.NearestFrameIndex(aStart, Math.Min(tA, Math.Max(0.0, aDur)));
                double tB = (double)k / fps;
                int idxB = GifEffectWindow.NearestFrameIndex(bStart, tB);
                double t = n == 1 ? 1.0 : (double)k / (n - 1);

                MagickImage frame;
                switch (settings.Style)
                {
                    case MorphStyle.TileFlip:
                        frame = TileFlipRenderer.RenderFrame(aSrc[idxA], bFit[idxB], t, grid, settings);
                        break;
                    case MorphStyle.Spotlight:
                        frame = SpotlightRenderer.RenderFrame(aSrc[idxA], bFit[idxB], t, morph, spot);
                        break;
                    case MorphStyle.Jigsaw:
                        frame = JigsawRenderer.RenderFrame(aSrc[idxA], bFit[idxB], t, grid, settings);
                        break;
                    case MorphStyle.Brick:
                        frame = BrickRenderer.RenderFrame(aSrc[idxA], bFit[idxB], t, brick);
                        break;
                    case MorphStyle.WaterFill:
                        // Fill rises over the morph window (t); te (elapsed seconds) animates the bubbles.
                        frame = WaterRenderer.RenderFrame(aSrc[idxA], bFit[idxB], t, (double)k / fps, water);
                        break;
                    default:
                        frame = RaindropRevealRenderer.RenderFrame(aSrc[idxA], bFit[idxB], t, drops, settings.SoftEdge);
                        break;
                }
                frame.AnimationDelay = (uint)delay;
                frame.AnimationTicksPerSecond = 100;
                frame.GifDisposeMethod = GifDisposeMethod.Background;
                result.Add(frame);
                ReportMorph(mainForm, ++built, totalFrames);
            }

            // Phase 3: B's remaining footage, at B's native timing.
            for (int i = 0; i < bFit.Count; i++)
            {
                if (bStart[i] < morph) continue;
                var frame = (MagickImage)bFit[i].Clone();
                frame.ResetPage();
                frame.AnimationDelay = bFit[i].AnimationDelay;
                frame.AnimationTicksPerSecond = bTicks;
                frame.GifDisposeMethod = GifDisposeMethod.Background;
                result.Add(frame);
                ReportMorph(mainForm, ++built, totalFrames);
            }

            return result;
        }

        private static void ReportMorph(GifToolMainForm mainForm, int built, int total)
        {
            if (total <= 0) total = 1;
            if (built % 5 == 0 || built == total)
            {
                SetProgressBar(mainForm.pBarTaskStatus, built * 100 / total, 100);
                SetStatusText(mainForm, SteamGifCropper.Properties.Resources.Status_MorphBuilding);
            }
        }

        #endregion
    }
}
