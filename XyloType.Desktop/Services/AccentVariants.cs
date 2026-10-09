using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Styling;

using Xylocopadream.UI.Avalonia;

namespace XyloType.Desktop.Services;

/// <summary>
/// Colors derived from the main one, missing from <see cref="XdAccent"/> (to move into the library):
/// <list type="bullet">
/// <item>two neighbors of its hue (±45°) for the badges of the generated exercises, and the end of the gradient of
/// the launch button;</item>
/// <item>what stays readable with a very light main color (a bright green, a yellow): the text and icons drawn on it
/// (white, or dark), and the main color itself darkened for the icons drawn on the light selection and for the links.</item>
/// </list>
/// Written in the application resources for both themes, again at each change of color (after <see cref="XdAccent"/>).
/// </summary>
public static class AccentVariants
{
    public const string Alternate1 = "App.AccentAlternate1";
    public const string Alternate2 = "App.AccentAlternate2";
    public const string GradientEnd = "App.AccentGradientEnd";

    /// <summary>Text and icons on the main color: white, or dark when white does not read on it.</summary>
    public const string OnAccent = "App.OnAccent";

    /// <summary>The main color for lines and icons on the page or on a selection, contrasted enough.</summary>
    public const string AccentInk = "App.AccentInk";

    // WCAG: 3:1 for icons and large text
    private const double MinContrast = 3.0;

    private static readonly Color s_darkInk = Color.Parse("#1E1F22");

    public static void Apply()
    {
        if (global::Avalonia.Application.Current is not { } app)
            return;

        Color accent = XdAccent.Current;
        foreach ((ThemeVariant variant, bool dark) in new[] { (ThemeVariant.Dark, true), (ThemeVariant.Light, false) })
        {
            if (!app.Resources.ThemeDictionaries.TryGetValue(variant, out var provider) || provider is not ResourceDictionary resources)
            {
                resources = [];
                app.Resources.ThemeDictionaries[variant] = resources;
            }

            resources[Alternate1] = new SolidColorBrush(Alternate(accent, 45, dark));
            resources[Alternate2] = new SolidColorBrush(Alternate(accent, -45, dark));
            resources[GradientEnd] = Turn(accent, 40);

            Color main = resources.TryGetResource("Xd.Accent", variant, out object? brush) && brush is ISolidColorBrush solid
                ? solid.Color
                : accent;
            Color onAccent = Contrast(Colors.White, main) >= MinContrast ? Colors.White : s_darkInk;
            resources[OnAccent] = new SolidColorBrush(onAccent);
            resources["AccentButtonForeground"] = new SolidColorBrush(onAccent);
            resources["AccentButtonForegroundPointerOver"] = new SolidColorBrush(onAccent);
            resources["AccentButtonForegroundPressed"] = new SolidColorBrush(onAccent);

            // on the light theme the selection is the main color faded: the icon on it takes a darker main color
            Color selection = resources.TryGetResource("Xd.Selected", variant, out object? selected) && selected is ISolidColorBrush selectedBrush
                ? selectedBrush.Color
                : (dark ? Color.Parse("#2B2D30") : Colors.White);
            Color ink = Readable(main, selection, dark);
            resources[AccentInk] = new SolidColorBrush(ink);
            if (!dark)
                resources["Xd.IconOnSelected"] = new SolidColorBrush(ink);

            // the links (and the best scores) on the page
            Color page = dark ? s_darkInk : Colors.White;
            foreach (string link in new[] { "Xd.Link", "Xd.LinkHover" })
            {
                if (resources.TryGetResource(link, variant, out object? linkBrush) && linkBrush is ISolidColorBrush linkColor)
                    resources[link] = new SolidColorBrush(Readable(linkColor.Color, page, dark));
            }
        }
    }

    /// <summary>
    /// The accent with its hue turned, well saturated, then darkened until a white text reads on it
    /// (a yellow or a cyan is much brighter than a blue at the same lightness); a bit lighter on the dark theme.
    /// </summary>
    private static Color Alternate(Color accent, double degrees, bool dark)
    {
        HslColor hsl = accent.ToHsl();
        double saturation = Math.Max(hsl.S, 0.55);
        double lightness = dark ? 0.6 : 0.47;
        double maxLuminance = dark ? 0.25 : 0.2;

        Color color = HslColor.ToRgb(TurnHue(hsl.H, degrees), saturation, lightness);
        while (RelativeLuminance(color) > maxLuminance && lightness > 0.2)
        {
            lightness -= 0.02;
            color = HslColor.ToRgb(TurnHue(hsl.H, degrees), saturation, lightness);
        }

        return color;
    }

    /// <summary>
    /// <paramref name="color"/> darkened (light theme) or lightened (dark theme), same hue, until it reads on
    /// <paramref name="background"/>.
    /// </summary>
    private static Color Readable(Color color, Color background, bool dark)
    {
        HslColor hsl = color.ToHsl();
        double lightness = hsl.L;
        while (Contrast(color, background) < MinContrast && lightness is > 0.15 and < 0.9)
        {
            lightness += dark ? 0.02 : -0.02;
            color = HslColor.ToRgb(hsl.H, hsl.S, lightness);
        }

        return color;
    }

    private static Color Turn(Color color, double degrees)
    {
        HslColor hsl = color.ToHsl();
        return HslColor.ToRgb(TurnHue(hsl.H, degrees), hsl.S, Math.Min(hsl.L, 0.6));
    }

    private static double TurnHue(double hue, double degrees) => ((hue + degrees) % 360 + 360) % 360;

    /// <summary>
    /// WCAG contrast ratio, from 1 (same luminance) to 21 (black on white).
    /// </summary>
    private static double Contrast(Color a, Color b)
    {
        double la = RelativeLuminance(a);
        double lb = RelativeLuminance(b);
        return (Math.Max(la, lb) + 0.05) / (Math.Min(la, lb) + 0.05);
    }

    /// <summary>
    /// WCAG relative luminance, from 0 (black) to 1 (white).
    /// </summary>
    private static double RelativeLuminance(Color color)
    {
        static double Linear(byte channel)
        {
            double c = channel / 255.0;
            return c <= 0.03928 ? c / 12.92 : Math.Pow((c + 0.055) / 1.055, 2.4);
        }

        return 0.2126 * Linear(color.R) + 0.7152 * Linear(color.G) + 0.0722 * Linear(color.B);
    }
}
