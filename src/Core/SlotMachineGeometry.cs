using System;

namespace GifProcessorApp
{
    // Pure, dependency-free slot-machine reel math so it can be linked into the test project
    // (which uses a GifProcessor stub instead of the Magick-heavy real class).
    //
    // Model: each reel (= a Steam column) wrap-scrolls its own column content vertically and
    // decelerates (ease-out cubic, fast -> slow, no abrupt stop) to land on the aligned image at
    // its own randomized stop frame; then it overshoots and wobbles back ("bounce") before locking.
    // The caller randomizes each reel's stop frame and spin count (so which reel stops first, and
    // which spins longest, is non-deterministic), then feeds the per-reel values in here. The
    // returned value is the vertical offset for MagickImage.Roll(0, offset).
    public static class SlotMachineGeometry
    {
        // Standard ease-out cubic: fast start, slow finish. Clamped to [0, 1].
        public static double EaseOutCubic(double t)
        {
            if (t <= 0.0) return 0.0;
            if (t >= 1.0) return 1.0;
            double inv = 1.0 - t;
            return 1.0 - inv * inv * inv;
        }

        // Applies a +/- variancePercent swing to baseValue. rand01 in [0,1] maps linearly to
        // [-variancePercent, +variancePercent]; 0.5 means no change. Pure so it stays testable —
        // the caller supplies randomness via Random.NextDouble().
        public static double ApplyVariance(double baseValue, double variancePercent, double rand01)
        {
            if (variancePercent < 0.0) variancePercent = 0.0;
            if (rand01 < 0.0) rand01 = 0.0;
            if (rand01 > 1.0) rand01 = 1.0;
            double delta = (rand01 * 2.0 - 1.0) * (variancePercent / 100.0);
            return baseValue * (1.0 + delta);
        }

        // Vertical wrap offset (in [0, canvasHeight)) for a reel at a given frame.
        //   - spin phase  [0, reelStopFrame): decelerating wrap-scroll, ends near 0.
        //   - bounce phase [reelStopFrame, reelStopFrame+overshootFrames): a single damped up/down
        //     wobble around 0 (overshoot then settle).
        //   - locked: returns 0 once frameIndex reaches reelStopFrame + overshootFrames.
        // topToBottom flips the apparent scroll direction.
        public static int ReelOffsetY(int frameIndex, int reelStopFrame, int spins, int canvasHeight,
                                      bool topToBottom, int overshootFrames)
        {
            if (canvasHeight <= 0) return 0;
            if (spins < 1) spins = 1;
            if (reelStopFrame < 1) reelStopFrame = 1;
            if (overshootFrames < 0) overshootFrames = 0;

            int settleFrame = reelStopFrame + overshootFrames;
            if (frameIndex >= settleFrame) return 0; // locked

            int rawOffset;
            if (frameIndex < reelStopFrame)
            {
                double localT = (double)frameIndex / reelStopFrame; // [0, 1)
                double eased = EaseOutCubic(localT);
                double distance = spins * (double)canvasHeight * (1.0 - eased);
                rawOffset = (int)Math.Round(distance);
            }
            else
            {
                // Overshoot/bounce: a single damped up-down wobble that settles back to 0.
                double bp = (double)(frameIndex - reelStopFrame) / overshootFrames; // [0, 1)
                int amplitude = Math.Max(2, canvasHeight / 25);
                double wobble = Math.Sin(bp * Math.PI * 2.0) * (1.0 - bp);
                rawOffset = (int)Math.Round(amplitude * wobble);
            }

            int offset = ((rawOffset % canvasHeight) + canvasHeight) % canvasHeight;
            if (!topToBottom)
            {
                offset = (canvasHeight - offset) % canvasHeight;
            }
            return offset;
        }
    }
}
