namespace XyloType.Desktop.Composition;

/// <summary>
/// Where the desktop app keeps its data: its own folder, apart from the MAUI app
/// (the exercise files of Documents\XyloType are shared by both).
/// </summary>
public static class AppPaths
{
    public static string Root { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Xylocopadream",
        "XyloType");

    /// <summary>
    /// Words, users and their results.
    /// </summary>
    public static string Database => Path.Combine(Root, "xylotype.db3");

    /// <summary>
    /// Settings of the user (theme, color, sounds, last user...).
    /// </summary>
    public static string Settings => Path.Combine(Root, "settings.json");

    public static string Logs => Path.Combine(Root, "logs");

    /// <summary>
    /// The sounds, scores and themes, next to the program.
    /// </summary>
    public static string Assets => Path.Combine(AppContext.BaseDirectory, "Assets");

    public static void EnsureCreated()
    {
        Directory.CreateDirectory(Root);
        Directory.CreateDirectory(Logs);
    }
}
