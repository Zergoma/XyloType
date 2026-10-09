using System.Globalization;

using XyloType.Application.Interfaces;
using XyloType.Application.Models.Themes;

namespace XyloType.Application.Services;

/// <summary>
/// A continuous scale: the hue turns from green to amber, then to red, at a constant saturation and lightness
/// (no muddy brown in between), lighter on the dark theme to stand out on its background.
/// </summary>
public class StatColorScale : IStatColorScale
{
    private const double GoodHue = 150;
    private const double MiddleHue = 42;
    private const double BadHue = 4;

    public string GetHexColor(double badness, ThemeState themeState)
    {
        double t = Math.Clamp(double.IsNaN(badness) ? 0 : badness, 0, 1);

        // the first half goes green to amber, the second amber to red
        double hue = t < 0.5
            ? GoodHue + (MiddleHue - GoodHue) * (t / 0.5)
            : MiddleHue + (BadHue - MiddleHue) * ((t - 0.5) / 0.5);

        (double saturation, double lightness) = themeState == ThemeState.Dark ? (0.70, 0.60) : (0.72, 0.46);
        return ToHex(hue, saturation, lightness);
    }

    private static string ToHex(double hue, double saturation, double lightness)
    {
        double c = (1 - Math.Abs(2 * lightness - 1)) * saturation;
        double x = c * (1 - Math.Abs(hue / 60 % 2 - 1));
        double m = lightness - c / 2;

        (double r, double g, double b) = hue switch
        {
            < 60 => (c, x, 0.0),
            < 120 => (x, c, 0.0),
            < 180 => (0.0, c, x),
            < 240 => (0.0, x, c),
            < 300 => (x, 0.0, c),
            _ => (c, 0.0, x),
        };

        return string.Create(CultureInfo.InvariantCulture,
            $"#{Byte(r + m):X2}{Byte(g + m):X2}{Byte(b + m):X2}");
    }

    private static int Byte(double value) => (int)Math.Round(Math.Clamp(value, 0, 1) * 255);
}
