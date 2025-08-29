using System;
using System.IO;

namespace GifProcessorApp
{
    public static class GifProcessor
    {
        private static readonly (int Start, int End)[] Ranges766 = { (0, 149), (154, 303), (308, 457), (462, 611), (616, 765) };
        private static readonly (int Start, int End)[] Ranges774 = { (0, 149), (155, 305), (311, 461), (467, 617), (623, 773) };
        private const uint SupportedWidth1 = 766;
        private const uint SupportedWidth2 = 774;

        private static bool IsValidCanvasWidth(uint width) => width == SupportedWidth1 || width == SupportedWidth2;

        private static (int Start, int End)[] GetCropRanges(uint canvasWidth)
        {
            return canvasWidth == SupportedWidth1 ? Ranges766 : Ranges774;
        }

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
    }
}
