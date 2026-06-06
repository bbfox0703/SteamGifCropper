using System;
using System.Collections.Generic;
using System.IO;

namespace SteamGifCropper
{
    /// <summary>
    /// Validates image files before passing them to ImageMagick by checking magic bytes.
    /// Prevents malicious files disguised with wrong extensions from reaching format parsers.
    /// </summary>
    public static class ImageInputValidator
    {
        private static readonly byte[] GifMagic87 = { 0x47, 0x49, 0x46, 0x38, 0x37, 0x61 }; // GIF87a
        private static readonly byte[] GifMagic89 = { 0x47, 0x49, 0x46, 0x38, 0x39, 0x61 }; // GIF89a
        private static readonly byte[] PngMagic = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
        private static readonly byte[] JpegMagic = { 0xFF, 0xD8, 0xFF };
        private static readonly byte[] BmpMagic = { 0x42, 0x4D }; // BM

        private const long MaxFileSizeBytes = 500L * 1024 * 1024; // 500 MB

        /// <summary>
        /// Validates that the file is a genuine GIF by checking magic bytes and file size.
        /// </summary>
        public static void ValidateGif(string filePath)
        {
            ValidateFileExists(filePath);
            ValidateFileSize(filePath);

            byte[] header = ReadHeader(filePath, 6);
            if (!StartsWith(header, GifMagic87) && !StartsWith(header, GifMagic89))
            {
                throw new InvalidOperationException(
                    string.Format(SteamGifCropper.Properties.Resources.Error_InvalidFileFormat, Path.GetFileName(filePath), "GIF"));
            }
        }

        /// <summary>
        /// Validates that the file is a genuine image (GIF, PNG, JPEG, or BMP) by checking magic bytes.
        /// </summary>
        public static void ValidateImage(string filePath)
        {
            ValidateFileExists(filePath);
            ValidateFileSize(filePath);

            byte[] header = ReadHeader(filePath, 8);

            if (StartsWith(header, GifMagic87) || StartsWith(header, GifMagic89))
                return;
            if (StartsWith(header, PngMagic))
                return;
            if (StartsWith(header, JpegMagic))
                return;
            if (StartsWith(header, BmpMagic))
                return;

            throw new InvalidOperationException(
                string.Format(SteamGifCropper.Properties.Resources.Error_InvalidFileFormat, Path.GetFileName(filePath), "GIF, PNG, JPEG, BMP"));
        }

        /// <summary>
        /// Validates multiple GIF files.
        /// </summary>
        public static void ValidateGifs(IEnumerable<string> filePaths)
        {
            foreach (string path in filePaths)
            {
                ValidateGif(path);
            }
        }

        private static void ValidateFileExists(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                throw new ArgumentException(SteamGifCropper.Properties.Resources.Error_SelectGif);
            if (!File.Exists(filePath))
                throw new FileNotFoundException(
                    string.Format(SteamGifCropper.Properties.Resources.Error_FileNotFound, filePath), filePath);
        }

        private static void ValidateFileSize(string filePath)
        {
            var fileInfo = new FileInfo(filePath);
            if (fileInfo.Length == 0)
            {
                throw new InvalidOperationException(
                    string.Format(SteamGifCropper.Properties.Resources.Error_InvalidFileFormat, Path.GetFileName(filePath), "GIF"));
            }
            if (fileInfo.Length > MaxFileSizeBytes)
            {
                throw new InvalidOperationException(
                    string.Format(SteamGifCropper.Properties.Resources.Error_FileTooLarge, Path.GetFileName(filePath), MaxFileSizeBytes / (1024 * 1024)));
            }
        }

        private static byte[] ReadHeader(string filePath, int length)
        {
            byte[] header = new byte[length];
            using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                int bytesRead = fs.Read(header, 0, length);
                if (bytesRead < length)
                {
                    throw new InvalidOperationException(
                        string.Format(SteamGifCropper.Properties.Resources.Error_InvalidFileFormat, Path.GetFileName(filePath), "GIF"));
                }
            }
            return header;
        }

        private static bool StartsWith(byte[] data, byte[] prefix)
        {
            if (data.Length < prefix.Length) return false;
            for (int i = 0; i < prefix.Length; i++)
            {
                if (data[i] != prefix[i]) return false;
            }
            return true;
        }
    }
}
