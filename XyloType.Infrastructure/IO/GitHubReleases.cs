namespace XyloType.Infrastructure.IO;

/// <summary>
/// Files published as the assets of a release of the GitHub repository: a fixed address per release tag,
/// whatever the releases of the app.
/// </summary>
internal static class GitHubReleases
{
    // one client for the app: sockets are reused
    public static HttpClient Http { get; } = new()
    {
        Timeout = TimeSpan.FromMinutes(5),
        DefaultRequestHeaders = { { "User-Agent", "XyloType" } },
    };

    /// <summary>
    /// Address of the assets of a release, ending with a slash.
    /// </summary>
    public static string AssetsUrl(string tag) => $"https://github.com/Zergoma/XyloType/releases/download/{tag}/";
}
