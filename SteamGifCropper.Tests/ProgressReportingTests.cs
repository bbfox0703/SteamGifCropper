using GifProcessorApp;
using System.IO;
using Xunit;

namespace SteamGifCropper.Tests;

public class ProgressReportingTests
{
    [Fact]
    public void SplitGif_ReportsExpectedProgress()
    {
        string tempDir = Directory.CreateTempSubdirectory().FullName;
        try
        {
            string input = GifTestHelper.CreateGradientGif(tempDir, 766, 100, 2, "red", "black");
            var form = new GifToolMainForm();
            GifProcessor.SplitGif(input, tempDir, form);
            var values = form.pBarTaskStatus.Values;
            Assert.Contains(50, values);
            Assert.Equal(100, values[^1]);
            Assert.Equal("10/10 (100%)", form.lblStatus.Text);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }
}
