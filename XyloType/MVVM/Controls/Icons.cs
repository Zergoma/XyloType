using Microsoft.Maui.Controls.Shapes;

namespace XyloType.MVVM.Controls;

/// <summary>
/// Vector icons for <see cref="IconButton"/>, filled shapes on a 24 x 24 grid.
/// Paths from Google Material Icons (Apache License 2.0).
/// </summary>
public static class Icons
{
    private static readonly PathGeometryConverter s_converter = new();

    /// <summary>
    /// "skip_previous": bar then triangle pointing left.
    /// </summary>
    public static Geometry Previous { get; } = Parse("M6 6h2v12H6zm3.5 6l8.5 6V6z");

    /// <summary>
    /// "skip_next": triangle pointing right then bar.
    /// </summary>
    public static Geometry Next { get; } = Parse("M6 18l8.5-6L6 6v12zM16 6v12h2V6h-2z");

    /// <summary>
    /// "block": circle crossed by a diagonal, "never again".
    /// </summary>
    public static Geometry Forbid { get; } = Parse(
        "M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zM4 12c0-4.42 3.58-8 8-8 1.85 0 3.55.63 4.9 1.69L5.69 16.9C4.63 15.55 4 13.85 4 12zm8 8c-1.85 0-3.55-.63-4.9-1.69L18.31 7.1C19.37 8.45 20 10.15 20 12c0 4.42-3.58 8-8 8z");

    /// <summary>
    /// "shuffle": crossing arrows, "random".
    /// </summary>
    public static Geometry Shuffle { get; } = Parse(
        "M10.59 9.17L5.41 4 4 5.41l5.17 5.17 1.42-1.41zM14.5 4l2.04 2.04L4 18.59 5.41 20 17.96 7.46 20 9.5V4h-5.5zm.33 9.41l-1.41 1.41 3.13 3.13L14.5 20H20v-5.5l-2.04 2.04-3.13-3.13z");

    /// <summary>
    /// "light_mode": sun, light theme.
    /// </summary>
    public static Geometry Sun { get; } = Parse(
        "M12 7c-2.76 0-5 2.24-5 5s2.24 5 5 5 5-2.24 5-5-2.24-5-5-5zM2 13h2c.55 0 1-.45 1-1s-.45-1-1-1H2c-.55 0-1 .45-1 1s.45 1 1 1zm18 0h2c.55 0 1-.45 1-1s-.45-1-1-1h-2c-.55 0-1 .45-1 1s.45 1 1 1zM11 2v2c0 .55.45 1 1 1s1-.45 1-1V2c0-.55-.45-1-1-1s-1 .45-1 1zm0 18v2c0 .55.45 1 1 1s1-.45 1-1v-2c0-.55-.45-1-1-1s-1 .45-1 1zM5.99 4.58c-.39-.39-1.03-.39-1.41 0-.39.39-.39 1.03 0 1.41l1.06 1.06c.39.39 1.03.39 1.41 0s.39-1.03 0-1.41L5.99 4.58zm12.37 12.37c-.39-.39-1.03-.39-1.41 0-.39.39-.39 1.03 0 1.41l1.06 1.06c.39.39 1.03.39 1.41 0 .39-.39.39-1.03 0-1.41l-1.06-1.06zm1.06-10.96c.39-.39.39-1.03 0-1.41-.39-.39-1.03-.39-1.41 0l-1.06 1.06c-.39.39-.39 1.03 0 1.41s1.03.39 1.41 0l1.06-1.06zM7.05 18.36c.39-.39.39-1.03 0-1.41-.39-.39-1.03-.39-1.41 0l-1.06 1.06c-.39.39-.39 1.03 0 1.41s1.03.39 1.41 0l1.06-1.06z");

    /// <summary>
    /// "dark_mode": crescent moon, dark theme.
    /// </summary>
    public static Geometry Moon { get; } = Parse(
        "M12 3c-4.97 0-9 4.03-9 9s4.03 9 9 9 9-4.03 9-9c0-.46-.04-.92-.1-1.36-.98 1.37-2.58 2.26-4.4 2.26-2.98 0-5.4-2.42-5.4-5.4 0-1.81.89-3.42 2.26-4.4-.44-.06-.9-.1-1.36-.1z");

    /// <summary>
    /// "desktop_windows": monitor, theme following the system.
    /// </summary>
    public static Geometry Monitor { get; } = Parse(
        "M20 2H4c-1.1 0-2 .9-2 2v12c0 1.1.9 2 2 2h7v2H8v2h8v-2h-3v-2h7c1.1 0 2-.9 2-2V4c0-1.1-.9-2-2-2zm0 14H4V4h16v12z");

    /// <summary>
    /// "play_arrow": start.
    /// </summary>
    public static Geometry Play { get; } = Parse("M8 5v14l11-7z");

    private static Geometry Parse(string data)
        => (Geometry)s_converter.ConvertFromInvariantString(data)!;
}
