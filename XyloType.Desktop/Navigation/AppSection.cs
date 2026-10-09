namespace XyloType.Desktop.Navigation;

/// <summary>
/// The sections of the app, reached from the navigation rail.
/// </summary>
public enum AppSection
{
    Home,
    Exercises,
    Words,
    Import,

    /// <summary>
    /// The users of the app, reached from the avatar of the right rail.
    /// </summary>
    Users,

    /// <summary>
    /// The exercise being typed, then its results: only while one is open (button at the top of the rail).
    /// </summary>
    Exercise
}
