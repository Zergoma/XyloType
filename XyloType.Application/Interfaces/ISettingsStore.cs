namespace XyloType.Application.Interfaces;

/// <summary>
/// The settings of the user, kept between sessions as key / value pairs
/// (MAUI preferences, a JSON file...): the services built on it do not depend on the UI framework.
/// Values are strings, booleans, integers or doubles.
/// </summary>
public interface ISettingsStore
{
    bool Contains(string key);

    T Get<T>(string key, T defaultValue);

    void Set<T>(string key, T value);

    void Remove(string key);
}
