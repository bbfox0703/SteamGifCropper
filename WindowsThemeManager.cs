using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

#nullable enable

namespace GifProcessorApp
{
    public static class WindowsThemeManager
    {
        private const int DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1 = 19;
        private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        [DllImport("uxtheme.dll", CharSet = CharSet.Unicode)]
        private static extern int SetWindowTheme(IntPtr hwnd, string? pszSubAppName, string? pszSubIdList);

        public static bool IsDarkModeEnabled(IRegistryProvider? registryProvider = null)
        {
            try
            {
                registryProvider ??= new RegistryProvider();
                var value = registryProvider.GetValue(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize", "AppsUseLightTheme");
                return value is int intValue && intValue == 0;
            }
            catch
            {
                return false;
            }
        }

        public static bool IsWindows10OrGreater()
        {
            return Environment.OSVersion.Version.Major >= 10;
        }

        public static void SetDarkModeForWindow(IntPtr handle, bool darkMode)
        {
            if (!IsWindows10OrGreater()) return;

            try
            {
                int darkModeValue = darkMode ? 1 : 0;
                
                // Try newer attribute first (Windows 11/20H1+)
                int result = DwmSetWindowAttribute(handle, DWMWA_USE_IMMERSIVE_DARK_MODE, ref darkModeValue, sizeof(int));
                
                // Fall back to older attribute for earlier Windows 10 versions
                if (result != 0)
                {
                    DwmSetWindowAttribute(handle, DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1, ref darkModeValue, sizeof(int));
                }
            }
            catch
            {
                // Silently fail on older systems
            }
        }

        public static void SetControlTheme(Control control, bool darkMode)
        {
            if (!IsWindows10OrGreater()) return;

            try
            {
                if (darkMode)
                {
                    SetWindowTheme(control.Handle, "DarkMode_Explorer", null);
                }
                else
                {
                    SetWindowTheme(control.Handle, null, null);
                }
            }
            catch
            {
                // Silently fail
            }
        }

        public static void ApplyThemeToControl(Control control, bool isDarkMode)
        {
            if (control is ContextMenuStrip contextMenu)
            {
                if (isDarkMode)
                {
                    ApplyDarkTheme(contextMenu);
                }
                else
                {
                    ApplyLightTheme(contextMenu);
                }

                foreach (ToolStripItem item in contextMenu.Items)
                {
                    ApplyThemeToToolStripItem(item, isDarkMode);
                }
            }
            else
            {
                if (isDarkMode)
                {
                    ApplyDarkTheme(control);
                }
                else
                {
                    ApplyLightTheme(control);
                }

                // Recursively apply to child controls
                foreach (Control childControl in control.Controls)
                {
                    ApplyThemeToControl(childControl, isDarkMode);
                }
            }
        }

        private static void ApplyDarkTheme(Control control)
        {
            // Dark theme colors
            var darkBackColor = Color.FromArgb(32, 32, 32);
            var darkForeColor = Color.FromArgb(255, 255, 255);
            var darkControlColor = Color.FromArgb(45, 45, 48);
            var darkBorderColor = Color.FromArgb(63, 63, 70);

            switch (control)
            {
                case Form form:
                    form.BackColor = darkBackColor;
                    form.ForeColor = darkForeColor;
                    SetDarkModeForWindow(form.Handle, true);
                    break;
                    
                case Button button:
                    button.BackColor = darkControlColor;
                    button.ForeColor = darkForeColor;
                    button.FlatStyle = FlatStyle.Flat;
                    button.FlatAppearance.BorderColor = darkBorderColor;
                    button.FlatAppearance.MouseOverBackColor = Color.FromArgb(62, 62, 64);
                    button.FlatAppearance.MouseDownBackColor = Color.FromArgb(27, 27, 28);
                    SetControlTheme(button, true);
                    break;

                case Label label:
                    label.BackColor = Color.Transparent;
                    label.ForeColor = darkForeColor;
                    break;

                case TextBox textBox:
                    textBox.BackColor = darkControlColor;
                    textBox.ForeColor = darkForeColor;
                    textBox.BorderStyle = BorderStyle.FixedSingle;
                    SetControlTheme(textBox, true);
                    break;

                case ProgressBar progressBar:
                    progressBar.BackColor = darkControlColor;
                    SetControlTheme(progressBar, true);
                    break;

                case CheckBox checkBox:
                    checkBox.BackColor = Color.Transparent;
                    checkBox.ForeColor = darkForeColor;
                    SetControlTheme(checkBox, true);
                    break;

                case NumericUpDown numericUpDown:
                    numericUpDown.BackColor = darkControlColor;
                    numericUpDown.ForeColor = darkForeColor;
                    SetControlTheme(numericUpDown, true);
                    break;

                case Panel panel:
                    panel.BackColor = darkBackColor;
                    break;

                case GroupBox groupBox:
                    groupBox.BackColor = Color.Transparent;
                    groupBox.ForeColor = darkForeColor;
                    break;

                case ContextMenuStrip menu:
                    menu.BackColor = darkControlColor;
                    menu.ForeColor = darkForeColor;
                    foreach (ToolStripItem item in menu.Items)
                    {
                        ApplyThemeToToolStripItem(item, true);
                    }
                    break;

                default:
                    if (control.BackColor != Color.Transparent && control.BackColor != SystemColors.Control)
                    {
                        control.BackColor = darkBackColor;
                    }
                    control.ForeColor = darkForeColor;
                    break;
            }
        }

        private static void ApplyLightTheme(Control control)
        {
            // Reset to system defaults
            switch (control)
            {
                case Form form:
                    form.BackColor = SystemColors.Control;
                    form.ForeColor = SystemColors.ControlText;
                    SetDarkModeForWindow(form.Handle, false);
                    break;
                    
                case Button button:
                    button.BackColor = SystemColors.Control;
                    button.ForeColor = SystemColors.ControlText;
                    button.FlatStyle = FlatStyle.System;
                    button.UseVisualStyleBackColor = true;
                    SetControlTheme(button, false);
                    break;

                case Label label:
                    label.BackColor = Color.Transparent;
                    label.ForeColor = SystemColors.ControlText;
                    break;

                case TextBox textBox:
                    textBox.BackColor = SystemColors.Window;
                    textBox.ForeColor = SystemColors.WindowText;
                    textBox.BorderStyle = BorderStyle.Fixed3D;
                    SetControlTheme(textBox, false);
                    break;

                case ProgressBar progressBar:
                    progressBar.BackColor = SystemColors.Control;
                    SetControlTheme(progressBar, false);
                    break;

                case CheckBox checkBox:
                    checkBox.BackColor = Color.Transparent;
                    checkBox.ForeColor = SystemColors.ControlText;
                    checkBox.UseVisualStyleBackColor = true;
                    SetControlTheme(checkBox, false);
                    break;

                case NumericUpDown numericUpDown:
                    numericUpDown.BackColor = SystemColors.Window;
                    numericUpDown.ForeColor = SystemColors.WindowText;
                    SetControlTheme(numericUpDown, false);
                    break;

                case Panel panel:
                    panel.BackColor = SystemColors.Control;
                    break;

                case GroupBox groupBox:
                    groupBox.BackColor = Color.Transparent;
                    groupBox.ForeColor = SystemColors.ControlText;
                    break;

                case ContextMenuStrip menu:
                    menu.BackColor = SystemColors.Control;
                    menu.ForeColor = SystemColors.ControlText;
                    foreach (ToolStripItem item in menu.Items)
                    {
                        ApplyThemeToToolStripItem(item, false);
                    }
                    break;

                default:
                    control.BackColor = SystemColors.Control;
                    control.ForeColor = SystemColors.ControlText;
                    break;
            }
        }

        private static void ApplyThemeToToolStripItem(ToolStripItem item, bool isDarkMode)
        {
            if (isDarkMode)
            {
                item.BackColor = Color.FromArgb(45, 45, 48);
                item.ForeColor = Color.FromArgb(255, 255, 255);
            }
            else
            {
                item.BackColor = SystemColors.Control;
                item.ForeColor = SystemColors.ControlText;
            }

            if (item is ToolStripMenuItem menuItem && menuItem.HasDropDownItems)
            {
                foreach (ToolStripItem dropDownItem in menuItem.DropDownItems)
                {
                    ApplyThemeToToolStripItem(dropDownItem, isDarkMode);
                }
            }
        }
    }
}