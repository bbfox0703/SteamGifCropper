using System;
using System.Drawing;
using System.Windows.Forms;

namespace GifProcessorApp
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            try
            {
                // .NET 8 modern high DPI support
                Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
                
                // Enable modern visual styles for Windows 10/11
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                
                // Enable Windows theming and modern controls
                if (WindowsThemeManager.IsWindows10OrGreater())
                {
                    // This enables modern Windows controls
                    Application.SetDefaultFont(SystemFonts.MessageBoxFont);
                }
                
                // Set global exception handler
                Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
                Application.ThreadException += Application_ThreadException;

                // Launch the main form
                Application.Run(new GifToolMainForm());
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Application startup failed: {ex.Message}\n\nStack trace:\n{ex.StackTrace}", 
                               "Startup Error", 
                               MessageBoxButtons.OK, 
                               MessageBoxIcon.Error);
            }
        }

        private static void Application_ThreadException(object sender, System.Threading.ThreadExceptionEventArgs e)
        {
            MessageBox.Show($"Unhandled exception: {e.Exception.Message}\n\nStack trace:\n{e.Exception.StackTrace}", 
                           "Error", 
                           MessageBoxButtons.OK, 
                           MessageBoxIcon.Error);
        }

    }
}
