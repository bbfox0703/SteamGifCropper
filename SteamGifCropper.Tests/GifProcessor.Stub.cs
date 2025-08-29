using System;
using System.IO;
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
    }
}
