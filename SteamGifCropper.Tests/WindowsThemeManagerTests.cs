using GifProcessorApp;
using Xunit;

namespace SteamGifCropper.Tests;

public class WindowsThemeManagerTests
{
    private class MockRegistryProvider : IRegistryProvider
    {
        private readonly object? _value;
        public MockRegistryProvider(object? value) => _value = value;
        public object? GetValue(string subKey, string valueName) => _value;
    }

    [Theory]
    [InlineData(0, true)]
    [InlineData(1, false)]
    public void IsDarkModeEnabled_ReturnsExpected(int registryValue, bool expected)
    {
        var provider = new MockRegistryProvider(registryValue);
        bool result = WindowsThemeManager.IsDarkModeEnabled(provider);
        Assert.Equal(expected, result);
    }
}

