namespace GifProcessorApp;

public static class WindowsThemeManager
{
    public static bool IsDarkModeEnabled(IRegistryProvider? registryProvider = null)
    {
        try
        {
            registryProvider ??= new RegistryProvider();
            var value = registryProvider.GetValue(@"Software\\Microsoft\\Windows\\CurrentVersion\\Themes\\Personalize", "AppsUseLightTheme");
            return value is int intValue && intValue == 0;
        }
        catch
        {
            return false;
        }
    }
}

