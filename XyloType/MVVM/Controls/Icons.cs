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

    private static Geometry Parse(string data)
        => (Geometry)s_converter.ConvertFromInvariantString(data)!;
}
