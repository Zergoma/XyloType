using System.Globalization;

namespace XyloType.ViewModels.Theme;

/// <summary>
/// Conversions between "#RRGGBB" and hue / saturation / value (hue in degrees, saturation and value from 0 to 1),
/// used by the color picker.
/// </summary>
public static class ColorMath
{
    /// <summary>
    /// "#RRGGBB" or "RRGGBB" (or the short "#RGB"), normalized to "#RRGGBB"; null when it is not a color.
    /// </summary>
    public static string? NormalizeHex(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return null;

        string hex = text.Trim().TrimStart('#');
        if (hex.Length == 3)
            hex = string.Concat(hex.Select(c => $"{c}{c}"));

        if (hex.Length != 6 || !int.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out _))
            return null;

        return "#" + hex.ToUpperInvariant();
    }

    public static (double Hue, double Saturation, double Value) HexToHsv(string hex)
    {
        int rgb = int.Parse(hex.TrimStart('#'), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        double r = ((rgb >> 16) & 0xFF) / 255.0;
        double g = ((rgb >> 8) & 0xFF) / 255.0;
        double b = (rgb & 0xFF) / 255.0;

        double max = Math.Max(r, Math.Max(g, b));
        double min = Math.Min(r, Math.Min(g, b));
        double delta = max - min;

        double hue = delta == 0 ? 0
            : max == r ? 60 * (((g - b) / delta) % 6)
            : max == g ? 60 * ((b - r) / delta + 2)
            : 60 * ((r - g) / delta + 4);
        if (hue < 0)
            hue += 360;

        return (hue, max == 0 ? 0 : delta / max, max);
    }

    public static string HsvToHex(double hue, double saturation, double value)
    {
        hue = ((hue % 360) + 360) % 360;
        saturation = Math.Clamp(saturation, 0, 1);
        value = Math.Clamp(value, 0, 1);

        double c = value * saturation;
        double x = c * (1 - Math.Abs(hue / 60 % 2 - 1));
        double m = value - c;

        (double r, double g, double b) = (int)(hue / 60) switch
        {
            0 => (c, x, 0.0),
            1 => (x, c, 0.0),
            2 => (0.0, c, x),
            3 => (0.0, x, c),
            4 => (x, 0.0, c),
            _ => (c, 0.0, x),
        };

        return $"#{ToByte(r + m):X2}{ToByte(g + m):X2}{ToByte(b + m):X2}";
    }

    private static int ToByte(double channel)
        => (int)Math.Round(Math.Clamp(channel, 0, 1) * 255);
}
