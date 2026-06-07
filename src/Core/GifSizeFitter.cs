using System;
using System.Threading.Tasks;

#nullable disable

namespace GifProcessorApp
{
    /// <summary>
    /// Searches gifsicle's parameter space to shrink a GIF to at most a target byte size while
    /// preserving as much quality as possible.
    ///
    /// Strategy (quality-first): pick the highest color count that can reach the target at maximum
    /// lossy, then binary-search the <em>lowest</em> lossy at that color count that still fits.
    /// This maximizes color fidelity first, then minimizes lossy artifacts — landing as close to
    /// (but under) the budget as the search granularity allows.
    ///
    /// The engine is decoupled from the gifsicle process via the <c>runner</c> delegate, so it can
    /// be unit-tested with a deterministic fake and reused with the real in-memory pipe.
    /// </summary>
    public static class GifSizeFitter
    {
        public class FitOptions
        {
            public long TargetBytes { get; set; }
            public int MaxAttempts { get; set; } = 14;
            public int OptimizeLevel { get; set; } = 3;
            public int Dither { get; set; } = 0;
            // Descending color counts to try. Color reduction is gifsicle's strongest size lever,
            // so this is the primary search axis; quality-first means trying more colors first.
            public int[] ColorLadder { get; set; } = { 256, 224, 192, 160, 128, 96, 64 };
            public int MaxLossy { get; set; } = 200;
            public int LossyStep { get; set; } = 10; // binary-search granularity for lossy
        }

        public class FitResult
        {
            public byte[] Data { get; set; }
            public long Bytes { get; set; }
            public int Colors { get; set; }
            public int Lossy { get; set; }
            public bool TargetMet { get; set; }
            public int Attempts { get; set; }
        }

        /// <param name="source">Source GIF bytes (held in memory; never mutated).</param>
        /// <param name="options">Target size and search bounds.</param>
        /// <param name="runner">Runs gifsicle with the given options on the source, returns output bytes.</param>
        /// <param name="progress">Optional per-attempt progress.</param>
        public static async Task<FitResult> FitAsync(
            byte[] source,
            FitOptions options,
            Func<byte[], GifsicleWrapper.GifsicleOptions, Task<byte[]>> runner,
            IProgress<(int attempt, int maxAttempts, long bytes, string status)> progress = null)
        {
            if (source == null || source.Length == 0)
                throw new ArgumentException("Source GIF bytes cannot be null or empty.", nameof(source));
            if (options == null) throw new ArgumentNullException(nameof(options));
            if (runner == null) throw new ArgumentNullException(nameof(runner));

            int step = Math.Max(1, options.LossyStep);
            int maxLossy = Math.Max(0, options.MaxLossy / step * step); // align to step grid

            int attempts = 0;
            FitResult smallest = null; // smallest result seen overall (fallback if target unreachable)

            async Task<FitResult> Try(int colors, int lossy)
            {
                attempts++;
                var opts = new GifsicleWrapper.GifsicleOptions
                {
                    Colors = colors,
                    Lossy = lossy,
                    OptimizeLevel = options.OptimizeLevel,
                    Dither = options.Dither
                };
                byte[] data = await runner(source, opts);
                var result = new FitResult
                {
                    Data = data,
                    Bytes = data.LongLength,
                    Colors = colors,
                    Lossy = lossy,
                    Attempts = attempts,
                    TargetMet = data.LongLength <= options.TargetBytes
                };
                if (smallest == null || result.Bytes < smallest.Bytes)
                {
                    smallest = result;
                }
                progress?.Report((attempts, options.MaxAttempts, result.Bytes,
                    string.Format("colors={0}, lossy={1} -> {2} KB", colors, lossy, result.Bytes / 1024)));
                return result;
            }

            // Phase 1: highest color count (quality-first) that fits at maximum lossy. Testing at
            // max lossy yields the smallest size for each color level, so it correctly decides which
            // color levels are feasible at all.
            int feasibleColors = -1;
            FitResult feasibleAtMaxLossy = null;
            foreach (int colors in options.ColorLadder)
            {
                if (attempts >= options.MaxAttempts) break;
                var r = await Try(colors, maxLossy);
                if (r.TargetMet)
                {
                    feasibleColors = colors;
                    feasibleAtMaxLossy = r;
                    break;
                }
            }

            if (feasibleColors < 0)
            {
                // Nothing reached the target, even at the most aggressive setting tried.
                smallest.TargetMet = false;
                smallest.Attempts = attempts;
                return smallest;
            }

            // Phase 2: binary-search the lowest lossy at feasibleColors that still fits.
            // Work in step-index space; hiIdx (== maxLossy) is known to fit.
            int loIdx = 0;
            int hiIdx = maxLossy / step;
            FitResult best = feasibleAtMaxLossy;
            while (loIdx < hiIdx && attempts < options.MaxAttempts)
            {
                int midIdx = (loIdx + hiIdx) / 2; // strictly < hiIdx, so maxLossy is never re-tested
                var r = await Try(feasibleColors, midIdx * step);
                if (r.TargetMet)
                {
                    best = r;     // a lower lossy also fits → tighten toward it
                    hiIdx = midIdx;
                }
                else
                {
                    loIdx = midIdx + 1; // mid too small (file too big) → need more lossy
                }
            }

            best.TargetMet = true;
            best.Attempts = attempts;
            return best;
        }
    }
}
