using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
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

        public static int PaletteQuantizeCallCount { get; set; }

        private static bool IsValidCanvasWidth(uint width) => width == SupportedWidth1 || width == SupportedWidth2;

        private static (int Start, int End)[] GetCropRanges(uint canvasWidth) => canvasWidth == SupportedWidth1 ? Ranges766 : Ranges774;

        private static void ModifyGifFile(string filePath, int adjustedHeight)
        {
            byte[] fileData = File.ReadAllBytes(filePath);
            if (fileData.Length < 10)
            {
                throw new InvalidOperationException($"Invalid GIF file: {filePath}");
            }

            fileData[^1] = 0x21;
            ushort heightValue = (ushort)adjustedHeight;
            fileData[8] = (byte)(heightValue & 0xFF);
            fileData[9] = (byte)((heightValue >> 8) & 0xFF);
            File.WriteAllBytes(filePath, fileData);
        }

        private static MagickImage BuildSharedPalette(IEnumerable<MagickImageCollection> collections, bool useFastPalette)
        {
            PaletteQuantizeCallCount++;
            var paletteSamples = new MagickImageCollection();
            try
            {
                foreach (var c in collections)
                {
                    if (c != null && c.Count > 0)
                    {
                        paletteSamples.Add((MagickImage)c[0].Clone());
                    }
                }

                var settings = new QuantizeSettings
                {
                    Colors = 256,
                    ColorSpace = ColorSpace.RGB,
                    DitherMethod = useFastPalette ? DitherMethod.No : DitherMethod.FloydSteinberg
                };

                if (useFastPalette)
                {
                    settings.TreeDepth = 5;
                }

                paletteSamples.Quantize(settings);
                return new MagickImage(paletteSamples[0]);
            }
            finally
            {
                paletteSamples.Dispose();
            }
        }

        private static MagickImageCollection[] SynchronizeToShortestDuration(MagickImageCollection[] collections, GifToolMainForm mainForm)
        {
            var durations = new double[collections.Length];
            for (int i = 0; i < collections.Length; i++)
            {
                durations[i] = collections[i].Sum(f => (double)f.AnimationDelay / f.AnimationTicksPerSecond);
            }

            double shortestDuration = durations.Min();
            var synced = new MagickImageCollection[collections.Length];

            for (int i = 0; i < collections.Length; i++)
            {
                synced[i] = new MagickImageCollection();
                if (Math.Abs(durations[i] - shortestDuration) < 0.0001)
                {
                    foreach (var frame in collections[i])
                    {
                        synced[i].Add(frame.Clone());
                    }
                }
                else
                {
                    double current = 0;
                    foreach (var frame in collections[i])
                    {
                        double frameDuration = (double)frame.AnimationDelay / frame.AnimationTicksPerSecond;
                        if (current + frameDuration <= shortestDuration)
                        {
                            synced[i].Add(frame.Clone());
                            current += frameDuration;
                        }
                        else
                        {
                            break;
                        }
                    }
                }

                foreach (var frame in synced[i])
                {
                    frame.GifDisposeMethod = GifDisposeMethod.Background;
                }
            }

            return synced;
        }

        public static async Task MergeMultipleGifs(List<string> gifPaths, string outputPath, GifToolMainForm mainForm, int targetFramerate = 15, bool useFastPalette = false)
        {
            if (gifPaths == null || gifPaths.Count < 2 || gifPaths.Count > 5)
                throw new ArgumentException("Invalid gif count");

            var collections = new List<MagickImageCollection>();
            try
            {
                foreach (var path in gifPaths)
                {
                    var c = new MagickImageCollection(path);
                    c.Coalesce();
                    collections.Add(c);
                }

                var widths = collections.Select(c => (int)c[0].Width).ToList();
                double shortestDuration = collections.Min(c => c.Sum(f => (double)f.AnimationDelay / f.AnimationTicksPerSecond));
                int targetFrameCount = Math.Max(1, (int)(shortestDuration * targetFramerate));
                int totalWidth = widths.Sum();
                int maxHeight = collections.Max(c => (int)c[0].Height);
                int ticksPerSecond = collections[0][0].AnimationTicksPerSecond;
                uint targetDelay = (uint)Math.Round((double)ticksPerSecond / targetFramerate);

                using var palette = BuildSharedPalette(collections, useFastPalette);
                using var merged = new MagickImageCollection();

                for (int frameIndex = 0; frameIndex < targetFrameCount; frameIndex++)
                {
                    var canvas = new MagickImage(MagickColors.Transparent, (uint)totalWidth, (uint)maxHeight);
                    int currentX = 0;
                    for (int gifIndex = 0; gifIndex < collections.Count; gifIndex++)
                    {
                        var col = collections[gifIndex];
                        double progress = (double)frameIndex / targetFrameCount;
                        int sourceIndex = Math.Min((int)(progress * col.Count), col.Count - 1);
                        canvas.Composite(col[sourceIndex], currentX, 0, CompositeOperator.Over);
                        currentX += widths[gifIndex];
                    }
                    canvas.AnimationDelay = targetDelay;
                    canvas.AnimationTicksPerSecond = ticksPerSecond;
                    canvas.GifDisposeMethod = GifDisposeMethod.Background;
                    merged.Add(canvas);
                }

                var mapSettings = new QuantizeSettings
                {
                    Colors = 256,
                    ColorSpace = ColorSpace.RGB,
                    DitherMethod = useFastPalette ? DitherMethod.No : DitherMethod.FloydSteinberg
                };

                foreach (MagickImage frame in merged)
                {
                    frame.Remap(palette, mapSettings);
                    frame.Format = MagickFormat.Gif;
                    frame.Settings.SetDefine(MagickFormat.Gif, "optimize-transparency", "true");
                }

                merged.Write(outputPath);
            }
            finally
            {
                foreach (var c in collections)
                {
                    c.Dispose();
                }
            }

            await Task.CompletedTask;
        }

        private static MagickImageCollection MergeGifsHorizontally(MagickImageCollection[] collections, GifToolMainForm mainForm, bool useFastPalette = false)
        {
            int maxHeight = collections.Max(c => (int)c[0].Height);
            using var palette = BuildSharedPalette(collections, useFastPalette);
            var merged = new MagickImageCollection();
            int maxFrames = collections.Max(c => c.Count);
            int[] xPositions = { 0, 153, 306, 460, 613 };

            for (int frameIndex = 0; frameIndex < maxFrames; frameIndex++)
            {
                var canvas = new MagickImage(MagickColors.Transparent, 766, (uint)maxHeight);
                for (int gifIndex = 0; gifIndex < 5; gifIndex++)
                {
                    var col = collections[gifIndex];
                    var frame = col[frameIndex % col.Count];
                    canvas.Composite(frame, xPositions[gifIndex], 0, CompositeOperator.Over);
                }
                var reference = collections[0][frameIndex % collections[0].Count];
                canvas.AnimationDelay = reference.AnimationDelay;
                canvas.AnimationTicksPerSecond = reference.AnimationTicksPerSecond;
                merged.Add(canvas);
            }

            foreach (var frame in merged)
            {
                frame.GifDisposeMethod = GifDisposeMethod.Background;
            }

            var mapSettings = new QuantizeSettings
            {
                Colors = 256,
                ColorSpace = ColorSpace.RGB,
                DitherMethod = useFastPalette ? DitherMethod.No : DitherMethod.FloydSteinberg
            };

            foreach (MagickImage frame in merged)
            {
                frame.Remap(palette, mapSettings);
            }

            return merged;
        }

        public static void ResizeGifTo766(string inputFilePath, string outputFilePath)
        {
            using var collection = new MagickImageCollection(inputFilePath);
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

        public static void SplitGif(string inputFilePath, string outputDirectory, int targetFramerate = 15, GifToolMainForm? mainForm = null)
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

            int totalFrames = collection.Count * ranges.Length;
            int currentFrame = 0;

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

                    currentFrame++;
                    if (mainForm != null)
                    {
                        int percent = (int)Math.Min((double)currentFrame / totalFrames * 100, 100);
                        mainForm.pBarTaskStatus.Value = percent;
                        mainForm.lblStatus.Text = $"{currentFrame}/{totalFrames} ({percent}%)";
                    }
                }

                foreach (var frame in partCollection)
                {
                    frame.Settings.SetDefine("compress", "LZW");
                }

                string outputFile = Path.Combine(outputDirectory, $"{Path.GetFileNameWithoutExtension(inputFilePath)}_Part{i + 1}.gif");
                partCollection.Write(outputFile);
            }
        }

        public static void ScrollStaticImage(string inputFilePath, string outputFilePath,
            ScrollDirection direction, int stepPixels, int durationSeconds, bool fullCycle, int moveCount, int targetFramerate = 15)
        {
            using var baseImage = new MagickImage(inputFilePath);
            int width = (int)baseImage.Width;
            int height = (int)baseImage.Height;

            int distance = direction switch
            {
                ScrollDirection.Up or ScrollDirection.Down => height,
                _ => width
            };

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

            int frames;
            int dx = 0, dy = 0;
            if (durationSeconds > 0)
            {
                frames = Math.Max(1, durationSeconds * targetFramerate);
            }
            else
            {
                dx = signX * stepPixels;
                dy = signY * stepPixels;
                if (fullCycle)
                {
                    int stepsX = dx != 0 ? (int)Math.Ceiling((double)width / Math.Abs(dx)) : 0;
                    int stepsY = dy != 0 ? (int)Math.Ceiling((double)height / Math.Abs(dy)) : 0;
                    frames = Math.Max(stepsX, stepsY);
                    if (frames <= 0) frames = 1;
                }
                else
                {
                    int maxMoves = moveCount;
                    if (dx != 0)
                        maxMoves = Math.Min(maxMoves, width / Math.Abs(dx));
                    if (dy != 0)
                        maxMoves = Math.Min(maxMoves, height / Math.Abs(dy));
                    frames = Math.Max(1, maxMoves);
                }
            }

            uint delay = (uint)Math.Round(100.0 / targetFramerate);
            using var collection = new MagickImageCollection();
            for (int i = 0; i < frames; i++)
            {
                var frame = baseImage.Clone();
                int offsetX, offsetY;
                if (durationSeconds > 0)
                {
                    int offset = (int)Math.Round((double)distance * i / frames);
                    offsetX = signX * offset;
                    offsetY = signY * offset;
                }
                else
                {
                    offsetX = dx * i;
                    offsetY = dy * i;
                }
                if (width > 0)
                {
                    offsetX %= width;
                    if (offsetX < 0) offsetX += width;
                }
                if (height > 0)
                {
                    offsetY %= height;
                    if (offsetY < 0) offsetY += height;
                }
                frame.Roll(offsetX, offsetY);
                frame.AnimationDelay = delay;
                frame.GifDisposeMethod = GifDisposeMethod.Background;
                collection.Add(frame);
            }

            var defines = new GifWriteDefines
            {
                RepeatCount = 0
            };

            collection.Write(outputFilePath, defines);
        }
    }
}
