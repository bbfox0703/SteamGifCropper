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
        private static readonly byte[] RiffMagic = { 0x52, 0x49, 0x46, 0x46 }; // "RIFF" (WebP container)
        private static readonly byte[] WebpFourCc = { 0x57, 0x45, 0x42, 0x50 }; // "WEBP" at offset 8
        private static readonly byte[] FtypMagic = { 0x66, 0x74, 0x79, 0x70 }; // "ftyp" at offset 4 (HEIF/HEIC box)
        // ISO-BMFF major brands we accept as HEIC/HEIF still images (bytes 8-11).
        private static readonly string[] HeifBrands = { "heic", "heix", "heim", "heis", "hevc", "hevx", "mif1", "msf1", "heif" };

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
        /// Validates that the file is a genuine image (GIF, PNG, JPEG, BMP, WebP, or HEIC/HEIF) by
        /// checking magic bytes.
        /// </summary>
        public static void ValidateImage(string filePath)
        {
            ValidateFileExists(filePath);
            ValidateFileSize(filePath);

            // 12 bytes covers every signature below (WebP/HEIC brands live at offset 8). Read up to
            // 12 without failing on shorter files so the short fixed-magic formats still validate.
            byte[] header = ReadHeaderUpTo(filePath, 12);

            if (StartsWith(header, GifMagic87) || StartsWith(header, GifMagic89))
                return;
            if (StartsWith(header, PngMagic))
                return;
            if (StartsWith(header, JpegMagic))
                return;
            if (StartsWith(header, BmpMagic))
                return;
            if (IsWebp(header))
                return;
            if (IsHeif(header))
                return;

            throw new InvalidOperationException(
                string.Format(SteamGifCropper.Properties.Resources.Error_InvalidFileFormat, Path.GetFileName(filePath), "GIF, PNG, JPEG, BMP, WebP, HEIC"));
        }

        // RIFF....WEBP : a WebP file (still or animated).
        private static bool IsWebp(byte[] header)
        {
            return header.Length >= 12 && StartsWith(header, RiffMagic)
                   && header[8] == WebpFourCc[0] && header[9] == WebpFourCc[1]
                   && header[10] == WebpFourCc[2] && header[11] == WebpFourCc[3];
        }

        // ....ftyp<brand> : an ISO-BMFF file whose major brand is a HEIC/HEIF still-image brand.
        private static bool IsHeif(byte[] header)
        {
            if (header.Length < 12) return false;
            for (int i = 0; i < FtypMagic.Length; i++)
            {
                if (header[4 + i] != FtypMagic[i]) return false;
            }
            string brand = System.Text.Encoding.ASCII.GetString(header, 8, 4);
            foreach (string b in HeifBrands)
            {
                if (brand == b) return true;
            }
            return false;
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

        // Reads up to <paramref name="length"/> bytes, returning however many are available (the
        // array is trimmed to the actual count). Unlike ReadHeader it does not throw on short files,
        // so callers can probe a long signature while shorter fixed magics still match.
        private static byte[] ReadHeaderUpTo(string filePath, int length)
        {
            byte[] buffer = new byte[length];
            int total = 0;
            using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                int read;
                while (total < length && (read = fs.Read(buffer, total, length - total)) > 0)
                {
                    total += read;
                }
            }
            if (total < length)
            {
                Array.Resize(ref buffer, total);
            }
            return buffer;
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
