namespace XyloType.Navigation;

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
    /// The exercise being typed, then its results: only while one is open (button at the top of the rail).
    /// </summary>
    Exercise
}
