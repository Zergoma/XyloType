using XyloType.Application.Interfaces;

namespace XyloType.Services;

/// <summary>
/// Puts the main color in the app resources, as dynamic resources used by the views and styles:
/// Accent, AccentHover, AccentPressed, AccentSoft (selected background), AccentSoftHover,
/// AccentText, AccentGradientEnd (end of the launch button gradient),
/// AccentSurface and AccentOnSurface (tinted boxes, like the exercise description, and their text),
/// AccentPageBg (behind the cards) and AccentTile* (exercise tiles), lightly tinted.
/// The variants follow the theme: lighter colors on the dark theme.
/// </summary>
public class MauiAccentColorService : IAccentColorService
{
    private const string AccentKey = "accent_color";

    // card backgrounds the soft accent is blended with
    private static readonly Color s_lightSurface = Color.FromArgb("#FFFFFF");
    private static readonly Color s_darkSurface = Color.FromArgb("#1E2229");

    private bool _followsTheme;

    public string DefaultAccent => "#2F6FEB";

    public string GetAccent()
        => Preferences.Default.Get(AccentKey, DefaultAccent);

    public void SetAccent(string hex)
    {
        Preferences.Default.Set(AccentKey, hex);
        Apply();
    }

    /// <summary>
    /// Applies the remembered color (at start), and again at each change of theme.
    /// </summary>
    public void Apply()
    {
        if (Microsoft.Maui.Controls.Application.Current is not { } app)
            return;

        if (!_followsTheme)
        {
            _followsTheme = true;
            app.RequestedThemeChanged += (_, _) => Apply();
        }

        Color accent = Color.FromArgb(GetAccent());
        bool dark = app.RequestedTheme == AppTheme.Dark;
        ResourceDictionary resources = app.Resources;

        // text and icons on the main color: white, or dark on a light color (yellow...)
        Color main = dark ? WithLuminosity(accent, Math.Min(accent.GetLuminosity() + 0.12, 0.8)) : accent;
        resources["AccentForeground"] = IsLight(main) ? Color.FromArgb("#1F2937") : Colors.White;

        if (dark)
        {
            resources["Accent"] = main;
            resources["AccentHover"] = WithLuminosity(main, Math.Min(main.GetLuminosity() + 0.08, 0.88));
            resources["AccentPressed"] = accent;
            resources["AccentSoft"] = Blend(accent, s_darkSurface, 0.28);
            resources["AccentSoftHover"] = Blend(accent, s_darkSurface, 0.36);
            resources["AccentText"] = WithLuminosity(main, Math.Min(main.GetLuminosity() + 0.06, 0.85));
            resources["AccentGradientEnd"] = WithLuminosity(ShiftHue(accent, 40), Math.Min(main.GetLuminosity(), 0.75));
            resources["AccentSurface"] = Blend(accent, s_darkSurface, 0.12);
            resources["AccentOnSurface"] = WithLuminosity(accent, 0.86);
            resources["AccentPageBg"] = Blend(accent, Color.FromArgb("#131417"), 0.04);
            resources["AccentTile"] = Blend(accent, Color.FromArgb("#24262B"), 0.08);
            resources["AccentTileHover"] = Blend(accent, Color.FromArgb("#24262B"), 0.14);
            resources["AccentTilePressed"] = Blend(accent, Color.FromArgb("#24262B"), 0.2);
            resources["AccentTileStroke"] = Blend(accent, Color.FromArgb("#33363D"), 0.1);
        }
        else
        {
            resources["Accent"] = accent;
            resources["AccentHover"] = WithLuminosity(accent, Math.Max(accent.GetLuminosity() - 0.07, 0.1));
            resources["AccentPressed"] = WithLuminosity(accent, Math.Max(accent.GetLuminosity() - 0.13, 0.08));
            resources["AccentSoft"] = Blend(accent, s_lightSurface, 0.13);
            resources["AccentSoftHover"] = Blend(accent, s_lightSurface, 0.2);
            resources["AccentText"] = accent;
            resources["AccentGradientEnd"] = ShiftHue(accent, 40);
            resources["AccentSurface"] = Blend(accent, s_lightSurface, 0.09);
            resources["AccentOnSurface"] = WithLuminosity(accent, 0.3);
            resources["AccentPageBg"] = Blend(accent, Color.FromArgb("#F4F5F7"), 0.045);
            resources["AccentTile"] = Blend(accent, Color.FromArgb("#F6F7F9"), 0.05);
            resources["AccentTileHover"] = Blend(accent, Color.FromArgb("#F6F7F9"), 0.1);
            resources["AccentTilePressed"] = Blend(accent, Color.FromArgb("#F6F7F9"), 0.15);
            resources["AccentTileStroke"] = Blend(accent, Color.FromArgb("#DADDE3"), 0.08);
        }
    }

    /// <summary>
    /// Perceived brightness (WCAG relative luminance): dark text reads better above 0.45.
    /// </summary>
    private static bool IsLight(Color color)
    {
        static double Linear(float channel)
            => channel <= 0.03928 ? channel / 12.92 : Math.Pow((channel + 0.055) / 1.055, 2.4);

        double luminance = 0.2126 * Linear(color.Red) + 0.7152 * Linear(color.Green) + 0.0722 * Linear(color.Blue);
        return luminance > 0.45;
    }

    private static Color WithLuminosity(Color color, double luminosity)
        => Color.FromHsla(color.GetHue(), color.GetSaturation(), Math.Clamp(luminosity, 0, 1));

    // hue of a Maui color goes from 0 to 1
    private static Color ShiftHue(Color color, double degrees)
        => Color.FromHsla((color.GetHue() + degrees / 360) % 1, color.GetSaturation(), color.GetLuminosity());

    /// <summary>
    /// The color laid over the surface with the given opacity.
    /// </summary>
    private static Color Blend(Color color, Color surface, double amount)
        => new(
            (float)(surface.Red + (color.Red - surface.Red) * amount),
            (float)(surface.Green + (color.Green - surface.Green) * amount),
            (float)(surface.Blue + (color.Blue - surface.Blue) * amount));
}
