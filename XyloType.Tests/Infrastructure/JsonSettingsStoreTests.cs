using FluentAssertions;

using Microsoft.Extensions.Logging.Abstractions;

using XyloType.Infrastructure.Settings;

namespace XyloType.Tests.Infrastructure;

public sealed class JsonSettingsStoreTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "xylotype-settings-" + Guid.NewGuid().ToString("N"));

    private string SettingsFile => Path.Combine(_folder, "settings.json");

    private JsonSettingsStore Open() => new(SettingsFile, NullLogger<JsonSettingsStore>.Instance);

    public void Dispose()
    {
        if (Directory.Exists(_folder))
            Directory.Delete(_folder, recursive: true);
    }

    [Fact]
    public void Values_OfEveryType_AreKeptAcrossSessions()
    {
        JsonSettingsStore store = Open();
        store.Set("text", "Dark");
        store.Set("flag", true);
        store.Set("count", 12);
        store.Set("volume", 0.35);

        JsonSettingsStore reopened = Open();

        reopened.Get("text", "System").Should().Be("Dark");
        reopened.Get("flag", false).Should().BeTrue();
        reopened.Get("count", 0).Should().Be(12);
        reopened.Get("volume", 0.0).Should().Be(0.35);
    }

    [Fact]
    public void MissingKey_GivesTheDefault_AndRemoveForgetsAKey()
    {
        JsonSettingsStore store = Open();
        store.Set("last_user_id", 3);

        store.Remove("last_user_id");

        store.Contains("last_user_id").Should().BeFalse();
        store.Get("last_user_id", -1).Should().Be(-1);
        Open().Contains("last_user_id").Should().BeFalse();
    }

    [Fact]
    public void ValueOfAnotherType_GivesTheDefault()
    {
        JsonSettingsStore store = Open();
        store.Set("count", "not a number");

        store.Get("count", 7).Should().Be(7);
    }

    [Fact]
    public void DamagedFile_IsSetAside_AndTheDefaultsAreUsed()
    {
        Directory.CreateDirectory(_folder);
        File.WriteAllText(SettingsFile, "{ not json");

        JsonSettingsStore store = Open();

        store.Get("theme", "System").Should().Be("System");
        File.Exists(SettingsFile + ".damaged").Should().BeTrue();
    }
}
