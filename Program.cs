using System;
using System.Windows.Forms;

namespace GifProcessorApp
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Launch the main form
            Application.Run(new GifToolMainForm());
        }
    }
}
