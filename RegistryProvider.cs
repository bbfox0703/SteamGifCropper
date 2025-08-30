using Microsoft.Win32;

#nullable enable

namespace GifProcessorApp;

public interface IRegistryProvider
{
    object? GetValue(string subKey, string valueName);
}

public class RegistryProvider : IRegistryProvider
{
    public object? GetValue(string subKey, string valueName)
    {
        using var key = Registry.CurrentUser.OpenSubKey(subKey);
        return key?.GetValue(valueName);
    }
}

