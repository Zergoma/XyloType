using System.Text.Json;
using System.Text.Json.Nodes;

using Microsoft.Extensions.Logging;

using XyloType.Application.Interfaces;

namespace XyloType.Infrastructure.Settings;

/// <summary>
/// The settings of the user in a JSON file (one object, key → value), read once, written at each change.
/// A damaged file is set aside and the defaults are used: the app always starts.
/// </summary>
public sealed class JsonSettingsStore : ISettingsStore
{
    private readonly string _path;
    private readonly ILogger<JsonSettingsStore> _logger;
    private readonly Lock _lock = new();
    private readonly JsonObject _values;

    public JsonSettingsStore(string path, ILogger<JsonSettingsStore> logger)
    {
        _path = path;
        _logger = logger;
        _values = Load();
    }

    public bool Contains(string key)
    {
        lock (_lock)
            return _values.ContainsKey(key);
    }

    public T Get<T>(string key, T defaultValue)
    {
        lock (_lock)
        {
            if (_values[key] is not JsonNode node)
                return defaultValue;

            try
            {
                return node.Deserialize<T>() ?? defaultValue;
            }
            catch (Exception ex) when (ex is JsonException or InvalidOperationException or FormatException)
            {
                // a value of another type (written by an older version): the default
                return defaultValue;
            }
        }
    }

    public void Set<T>(string key, T value)
    {
        lock (_lock)
        {
            _values[key] = JsonSerializer.SerializeToNode(value);
            Save();
        }
    }

    public void Remove(string key)
    {
        lock (_lock)
        {
            if (_values.Remove(key))
                Save();
        }
    }

    private JsonObject Load()
    {
        if (!File.Exists(_path))
            return [];

        try
        {
            return JsonNode.Parse(File.ReadAllText(_path)) as JsonObject ?? [];
        }
        catch (Exception ex) when (ex is JsonException or IOException)
        {
            _logger.LogWarning(ex, "Settings file {Path} unreadable: set aside, defaults used", _path);
            TryMove(_path, _path + ".damaged");
            return [];
        }
    }

    private void Save()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_path)!);

            // written next to it, then swapped: a crash never leaves half a file
            string temp = _path + ".tmp";
            File.WriteAllText(temp, _values.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
            MoveWithRetry(temp, _path);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _logger.LogWarning(ex, "Settings file {Path} not saved", _path);
        }
    }

    /// <summary>
    /// A file just written can be held a moment (an antivirus reading it): the swap is tried again.
    /// </summary>
    private static void MoveWithRetry(string from, string to)
    {
        for (int attempt = 1; ; attempt++)
        {
            try
            {
                File.Move(from, to, overwrite: true);
                return;
            }
            catch (Exception ex) when (attempt < MoveAttempts && ex is IOException or UnauthorizedAccessException)
            {
                Thread.Sleep(MoveRetryDelay);
            }
        }
    }

    private const int MoveAttempts = 5;
    private static readonly TimeSpan MoveRetryDelay = TimeSpan.FromMilliseconds(50);

    private static void TryMove(string from, string to)
    {
        try { File.Move(from, to, overwrite: true); } catch (IOException) { } catch (UnauthorizedAccessException) { }
    }
}
