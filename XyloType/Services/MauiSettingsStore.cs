using XyloType.Application.Interfaces;

namespace XyloType.Services;

/// <summary>
/// The settings of the app in the MAUI preferences.
/// </summary>
public class MauiSettingsStore : ISettingsStore
{
    public bool Contains(string key)
        => Preferences.Default.ContainsKey(key);

    public T Get<T>(string key, T defaultValue)
        => Preferences.Default.Get(key, defaultValue);

    public void Set<T>(string key, T value)
        => Preferences.Default.Set(key, value);

    public void Remove(string key)
        => Preferences.Default.Remove(key);
}
