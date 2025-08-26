using System;
using System.Windows.Forms;

namespace GifProcessorApp
{
    public partial class GifToolMainForm : Form
    {
        public int DitherMethod { get; private set; } = 0;
        
        public GifToolMainForm()
        {
            try
            {
                InitializeComponent();
                
                // Set initial state
                lblStatus.Text = "Ready";
                pBarTaskStatus.Visible = false;
                
                // Ensure proper form state
                this.WindowState = FormWindowState.Normal;
                this.StartPosition = FormStartPosition.CenterScreen;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to initialize form: {ex.Message}", 
                                "Initialization Error", 
                                MessageBoxButtons.OK, 
                                MessageBoxIcon.Error);
                throw;
            }
        }

        private void ExecuteWithErrorHandling(Action action, string operationName)
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"An error occurred during {operationName}: {ex.Message}",
                                "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnSplitGif_Click(object sender, EventArgs e)
        {
            ExecuteWithErrorHandling(() => GifProcessor.StartProcessing(this), "GIF splitting");
        }

        private void btnResizeGif766_Click(object sender, EventArgs e)
        {
            ExecuteWithErrorHandling(() => GifProcessor.ResizeGifTo766(this), "GIF resizing");
        }

        private void btnWriteTailByte_Click(object sender, EventArgs e)
        {
            ExecuteWithErrorHandling(() => GifProcessor.WriteTailByteForMultipleGifs(this), "tail byte modification");
        }

        private void btnSplitGIFWithReducedPalette_Click(object sender, EventArgs e)
        {
            ExecuteWithErrorHandling(() => GifProcessor.SplitGifWithReducedPalette(this), "palette reduction and splitting");
        }

        private void label2_Click(object sender, EventArgs e)
        {

        }

        private void radioBtnDNone_Click(object sender, EventArgs e) => DitherMethod = 0;
        private void radioBtnDro64_Click(object sender, EventArgs e) => DitherMethod = 1;
        private void radioBtnDo8_Click(object sender, EventArgs e) => DitherMethod = 2;
        private void radioBtnDDefault_Click(object sender, EventArgs e) => DitherMethod = 3;
    }
}
