using System.Collections.Concurrent;
using System.Globalization;

namespace XyloType.Converters;

public class StringToColorConverter : IValueConverter
{
    // the same few colors come back for every letter at each key: parsed once
    private static readonly ConcurrentDictionary<string, Color> s_colors = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string color)
        {
            return s_colors.GetOrAdd(color, Color.FromArgb);
        }

        return Colors.Transparent;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
