using System.Collections.Concurrent;
using System.Globalization;

using Avalonia.Data.Converters;
using Avalonia.Media;
using Avalonia.Media.Immutable;

namespace XyloType.Desktop.Converters;

/// <summary>
/// "#RRGGBB" (the colors of the typing themes and of the charts) to a brush.
/// </summary>
public sealed class HexToBrushConverter : IValueConverter
{
    public static HexToBrushConverter Instance { get; } = new();

    // the same few colors come back for every letter at each key: parsed once
    private static readonly ConcurrentDictionary<string, IBrush> s_brushes = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is string hex && !string.IsNullOrEmpty(hex)
            ? s_brushes.GetOrAdd(hex, h => Color.TryParse(h, out Color color) ? new ImmutableSolidColorBrush(color) : Brushes.Transparent)
            : Brushes.Transparent;

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
