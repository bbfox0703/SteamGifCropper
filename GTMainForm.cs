using System;
using System.Windows.Forms;

namespace GifProcessorApp
{
    public partial class GifToolMainForm : Form
    {
        public int ditherMethod = 0;
        public GifToolMainForm()
        {
            InitializeComponent();
        }

        // Event handler for the button to start GIF processing
        private void btnSplitGif_Click(object sender, EventArgs e)
        {
            try
            {
                GifProcessor.StartProcessing(this); // Call the processing function
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred: {ex.Message}",
                                "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnResizeGif766_Click(object sender, EventArgs e)
        {
            try
            {
                GifProcessor.ResizeGifTo766(this); // Call the resize method and pass the current form
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"An error occurred: {ex.Message}",
                                "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnWriteTailByte_Click(object sender, EventArgs e)
        {
            try
            {
                GifProcessor.WriteTailByteForMultipleGifs(this); // Call the method and pass the current form
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"An error occurred: {ex.Message}",
                                "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnSplitGIFWithReducedPalette_Click(object sender, EventArgs e)
        {
            try
            {
                GifProcessor.SplitGifWithReducedPalette(this);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"An error occurred: {ex.Message}",
                                "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void label2_Click(object sender, EventArgs e)
        {

        }

        private void radioBtnDNone_Click(object sender, EventArgs e)
        {
            ditherMethod = 0;
        }

        private void radioBtnDro64_Click(object sender, EventArgs e)
        {
            ditherMethod = 1;
        }

        private void radioBtnDo8_Click(object sender, EventArgs e)
        {
            ditherMethod = 2;
        }

        private void radioBtnDDefault_Click(object sender, EventArgs e)
        {
            ditherMethod = 3;
        }
    }
}
