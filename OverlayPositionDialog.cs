using System;
using System.Windows.Forms;

namespace GifProcessorApp
{
    public partial class OverlayPositionDialog : Form
    {
        public int OverlayX => (int)numX.Value;
        public int OverlayY => (int)numY.Value;

        public OverlayPositionDialog()
        {
            InitializeComponent();
            WindowsThemeManager.ApplyThemeToControl(this, WindowsThemeManager.IsDarkModeEnabled());
        }

        public OverlayPositionDialog(uint maxWidth, uint maxHeight) : this()
        {
            numX.Maximum = maxWidth > 0 ? maxWidth - 1 : 0;
            numY.Maximum = maxHeight > 0 ? maxHeight - 1 : 0;
        }
    }
}
