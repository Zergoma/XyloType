namespace XyloType.Application.Interfaces;

/// <summary>
/// Main color of the app (selections, launch button, switches...), chosen by the user and remembered.
/// </summary>
public interface IAccentColorService
{
    /// <summary>
    /// The color chosen by the user, as "#RRGGBB".
    /// </summary>
    string GetAccent();

    /// <summary>
    /// Applies the color to the whole app (adapted to the light and dark themes) and remembers it.
    /// </summary>
    void SetAccent(string hex);

    /// <summary>
    /// The color of the app before any choice.
    /// </summary>
    string DefaultAccent { get; }
}
