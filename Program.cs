using System;
using System.Drawing;
using System.Globalization;
using System.Configuration;
using System.Threading;
using System.Windows.Forms;
using ImageMagick;

namespace GifProcessorApp
{
    static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            try
            {
                // Configure ImageMagick resource limits, allowing overrides via config or command line
                ConfigureResourceLimits(args);

                // Test and configure OpenCL GPU acceleration
                TestAndConfigureOpenCL();

                // Initialize localization based on OS language
                InitializeLocalization();

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
                MessageBox.Show(string.Format(SteamGifCropper.Properties.Resources.Error_StartupFailed, ex.Message, ex.StackTrace),
                               SteamGifCropper.Properties.Resources.Title_StartupError,
                               MessageBoxButtons.OK,
                               MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Configure ImageMagick resource limits from app settings or command line arguments.
        /// </summary>
        private static void ConfigureResourceLimits(string[] args)
        {
            ulong memoryMb = GetAppSettingULong("ResourceLimits.MemoryMB", 1024UL);
            ulong diskMb = GetAppSettingULong("ResourceLimits.DiskMB", 4096UL);

            if (args != null)
            {
                foreach (var arg in args)
                {
                    if (arg.StartsWith("--memory-limit=", StringComparison.OrdinalIgnoreCase))
                    {
                        var value = arg.Substring("--memory-limit=".Length);
                        if (ulong.TryParse(value, out var parsed))
                        {
                            memoryMb = parsed;
                        }
                    }
                    else if (arg.StartsWith("--disk-limit=", StringComparison.OrdinalIgnoreCase))
                    {
                        var value = arg.Substring("--disk-limit=".Length);
                        if (ulong.TryParse(value, out var parsed))
                        {
                            diskMb = parsed;
                        }
                    }
                }
            }

            ResourceLimits.Memory = memoryMb * 1024UL * 1024UL;
            ResourceLimits.Disk = diskMb * 1024UL * 1024UL;
        }

        /// <summary>
        /// Helper to retrieve ulong values from App.config.
        /// </summary>
        private static ulong GetAppSettingULong(string key, ulong defaultValue)
        {
            try
            {
                var value = ConfigurationManager.AppSettings[key];
                if (!string.IsNullOrEmpty(value) && ulong.TryParse(value, out var result))
                {
                    return result;
                }
            }
            catch
            {
                // ignore configuration errors and fallback to default
            }
            return defaultValue;
        }

        private static void Application_ThreadException(object sender, System.Threading.ThreadExceptionEventArgs e)
        {
            MessageBox.Show(string.Format(SteamGifCropper.Properties.Resources.Error_UnhandledException, e.Exception.Message, e.Exception.StackTrace), 
                           SteamGifCropper.Properties.Resources.Title_Error, 
                           MessageBoxButtons.OK, 
                           MessageBoxIcon.Error);
        }

        /// <summary>
        /// Test and configure OpenCL GPU acceleration if available
        /// </summary>
        private static void TestAndConfigureOpenCL()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("Testing OpenCL GPU acceleration...");

                // Try to enable OpenCL - this will work if the Magick.NET build supports it
                // and the system has OpenCL-compatible devices
                OpenCL.IsEnabled = true;

                System.Diagnostics.Debug.WriteLine($"OpenCL IsEnabled: {OpenCL.IsEnabled}");

                if (OpenCL.IsEnabled)
                {
                    System.Diagnostics.Debug.WriteLine("OpenCL GPU acceleration is enabled");
                    System.Diagnostics.Debug.WriteLine("The first OpenCL operation will run a benchmark to determine optimal device");
                    System.Diagnostics.Debug.WriteLine("Benchmark results will be stored at: %LOCALAPPDATA%\\ImageMagick\\ImagemagickOpenCLDeviceProfile.xml");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("OpenCL is not available - either not supported in this build or no compatible devices found");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"OpenCL configuration failed: {ex.Message}");
                // Continue without OpenCL acceleration
            }
        }

        /// <summary>
        /// Initialize localization based on OS language detection
        /// </summary>
        private static void InitializeLocalization()
        {
            InitializeLocalization(null);
        }

        /// <summary>
        /// Initialize localization with specific culture or auto-detect
        /// </summary>
        /// <param name="forceCulture">Specific culture to use, or null for auto-detection</param>
        public static void InitializeLocalization(string forceCulture)
        {
            try
            {
                CultureInfo targetCulture;

                if (!string.IsNullOrEmpty(forceCulture))
                {
                    // Use forced culture
                    targetCulture = new CultureInfo(forceCulture);
                }
                else
                {
                    // Get the OS UI culture
                    var systemCulture = CultureInfo.InstalledUICulture;

                    // Determine target culture based on OS language
                    if (systemCulture.Name.StartsWith("zh-TW") || 
                        systemCulture.Name.StartsWith("zh-Hant") ||
                        systemCulture.Name == "zh-CHT")
                    {
                        // Traditional Chinese
                        targetCulture = new CultureInfo("zh-TW");
                    }
                    else if (systemCulture.Name.StartsWith("ja"))
                    {
                        // Japanese
                        targetCulture = new CultureInfo("ja");
                    }
                    else
                    {
                        // Default to English for all other languages
                        targetCulture = new CultureInfo("en");
                    }
                }

                // Set both current culture and UI culture
                Thread.CurrentThread.CurrentCulture = targetCulture;
                Thread.CurrentThread.CurrentUICulture = targetCulture;

                // Set default culture for new threads
                CultureInfo.DefaultThreadCurrentCulture = targetCulture;
                CultureInfo.DefaultThreadCurrentUICulture = targetCulture;

                // Ensure resources use the selected culture
                SteamGifCropper.Properties.Resources.Culture = targetCulture;

                // Debug logging
                System.Diagnostics.Debug.WriteLine($"Localization initialized: {targetCulture.Name} ({targetCulture.DisplayName})");
            }
            catch (Exception ex)
            {
                // If localization initialization fails, fallback to English
                try
                {
                    var fallbackCulture = new CultureInfo("en");
                    Thread.CurrentThread.CurrentCulture = fallbackCulture;
                    Thread.CurrentThread.CurrentUICulture = fallbackCulture;
                    CultureInfo.DefaultThreadCurrentCulture = fallbackCulture;
                    CultureInfo.DefaultThreadCurrentUICulture = fallbackCulture;
                }
                catch
                {
                    // If even fallback fails, continue with system defaults
                }

                // Log the error (in debug mode)
                System.Diagnostics.Debug.WriteLine($"Localization initialization failed: {ex.Message}");
            }
        }

    }
}
