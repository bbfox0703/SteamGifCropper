using System;
using System.IO;
using SteamGifCropper;

public class ImageInputValidatorTests : IDisposable
{
    private readonly string _tempDir;

    public ImageInputValidatorTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "ValidatorTests_" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, true);
    }

    private string CreateTempFile(string name, byte[] content)
    {
        string path = Path.Combine(_tempDir, name);
        File.WriteAllBytes(path, content);
        return path;
    }

    // GIF87a magic bytes
    private static readonly byte[] ValidGif87a = { 0x47, 0x49, 0x46, 0x38, 0x37, 0x61, 0x01, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00, 0x3B };
    // GIF89a magic bytes
    private static readonly byte[] ValidGif89a = { 0x47, 0x49, 0x46, 0x38, 0x39, 0x61, 0x01, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00, 0x3B };
    // PNG magic bytes
    private static readonly byte[] ValidPng = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00 };
    // JPEG magic bytes
    private static readonly byte[] ValidJpeg = { 0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46 };
    // BMP magic bytes
    private static readonly byte[] ValidBmp = { 0x42, 0x4D, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };

    #region ValidateGif

    [Fact]
    public void ValidateGif_WithValidGif87a_DoesNotThrow()
    {
        string path = CreateTempFile("valid87a.gif", ValidGif87a);
        ImageInputValidator.ValidateGif(path);
    }

    [Fact]
    public void ValidateGif_WithValidGif89a_DoesNotThrow()
    {
        string path = CreateTempFile("valid89a.gif", ValidGif89a);
        ImageInputValidator.ValidateGif(path);
    }

    [Fact]
    public void ValidateGif_WithRealTestGif_DoesNotThrow()
    {
        string path = Path.Combine("TestData", "small.gif");
        if (!File.Exists(path))
            return; // Skip if test data not available
        ImageInputValidator.ValidateGif(path);
    }

    [Fact]
    public void ValidateGif_WithPngFile_Throws()
    {
        string path = CreateTempFile("fake.gif", ValidPng);
        Assert.Throws<InvalidOperationException>(() => ImageInputValidator.ValidateGif(path));
    }

    [Fact]
    public void ValidateGif_WithJpegFile_Throws()
    {
        string path = CreateTempFile("fake.gif", ValidJpeg);
        Assert.Throws<InvalidOperationException>(() => ImageInputValidator.ValidateGif(path));
    }

    [Fact]
    public void ValidateGif_WithRandomBytes_Throws()
    {
        string path = CreateTempFile("random.gif", new byte[] { 0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07 });
        Assert.Throws<InvalidOperationException>(() => ImageInputValidator.ValidateGif(path));
    }

    [Fact]
    public void ValidateGif_WithSvgContent_Throws()
    {
        // SVG disguised as .gif - common attack vector for ImageMagick CVEs
        byte[] svgContent = System.Text.Encoding.UTF8.GetBytes("<svg xmlns=\"http://www.w3.org/2000/svg\"><text>malicious</text></svg>");
        string path = CreateTempFile("malicious.gif", svgContent);
        Assert.Throws<InvalidOperationException>(() => ImageInputValidator.ValidateGif(path));
    }

    [Fact]
    public void ValidateGif_WithEmptyFile_Throws()
    {
        string path = CreateTempFile("empty.gif", Array.Empty<byte>());
        Assert.Throws<InvalidOperationException>(() => ImageInputValidator.ValidateGif(path));
    }

    [Fact]
    public void ValidateGif_WithTooShortFile_Throws()
    {
        string path = CreateTempFile("short.gif", new byte[] { 0x47, 0x49 });
        Assert.Throws<InvalidOperationException>(() => ImageInputValidator.ValidateGif(path));
    }

    [Fact]
    public void ValidateGif_WithNonExistentFile_Throws()
    {
        Assert.Throws<FileNotFoundException>(() => ImageInputValidator.ValidateGif(Path.Combine(_tempDir, "nonexistent.gif")));
    }

    [Fact]
    public void ValidateGif_WithNullPath_Throws()
    {
        Assert.Throws<ArgumentException>(() => ImageInputValidator.ValidateGif(null!));
    }

    [Fact]
    public void ValidateGif_WithEmptyPath_Throws()
    {
        Assert.Throws<ArgumentException>(() => ImageInputValidator.ValidateGif(""));
    }

    #endregion

    #region ValidateImage

    [Fact]
    public void ValidateImage_WithGif_DoesNotThrow()
    {
        string path = CreateTempFile("test.gif", ValidGif89a);
        ImageInputValidator.ValidateImage(path);
    }

    [Fact]
    public void ValidateImage_WithPng_DoesNotThrow()
    {
        string path = CreateTempFile("test.png", ValidPng);
        ImageInputValidator.ValidateImage(path);
    }

    [Fact]
    public void ValidateImage_WithJpeg_DoesNotThrow()
    {
        string path = CreateTempFile("test.jpg", ValidJpeg);
        ImageInputValidator.ValidateImage(path);
    }

    [Fact]
    public void ValidateImage_WithBmp_DoesNotThrow()
    {
        string path = CreateTempFile("test.bmp", ValidBmp);
        ImageInputValidator.ValidateImage(path);
    }

    [Fact]
    public void ValidateImage_WithSvg_Throws()
    {
        byte[] svgContent = System.Text.Encoding.UTF8.GetBytes("<svg xmlns=\"http://www.w3.org/2000/svg\"></svg>");
        string path = CreateTempFile("malicious.png", svgContent);
        Assert.Throws<InvalidOperationException>(() => ImageInputValidator.ValidateImage(path));
    }

    [Fact]
    public void ValidateImage_WithPdfContent_Throws()
    {
        byte[] pdfContent = System.Text.Encoding.UTF8.GetBytes("%PDF-1.4 malicious content");
        string path = CreateTempFile("malicious.png", pdfContent);
        Assert.Throws<InvalidOperationException>(() => ImageInputValidator.ValidateImage(path));
    }

    [Fact]
    public void ValidateImage_WithRealPng_DoesNotThrow()
    {
        string path = Path.Combine("TestData", "Scroll_test_1.png");
        if (!File.Exists(path))
            return; // Skip if test data not available
        ImageInputValidator.ValidateImage(path);
    }

    #endregion

    #region ValidateGifs (batch)

    [Fact]
    public void ValidateGifs_WithAllValid_DoesNotThrow()
    {
        string path1 = CreateTempFile("a.gif", ValidGif89a);
        string path2 = CreateTempFile("b.gif", ValidGif87a);
        ImageInputValidator.ValidateGifs(new[] { path1, path2 });
    }

    [Fact]
    public void ValidateGifs_WithOneBadFile_Throws()
    {
        string goodPath = CreateTempFile("good.gif", ValidGif89a);
        string badPath = CreateTempFile("bad.gif", ValidPng);
        Assert.Throws<InvalidOperationException>(() => ImageInputValidator.ValidateGifs(new[] { goodPath, badPath }));
    }

    #endregion
}
