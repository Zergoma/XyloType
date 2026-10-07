using Xylocopadream.UI.Maui;

using XyloType.Application.Interfaces;

namespace XyloType.Services;

/// <summary>
/// The main color chosen by the user, remembered in the preferences and applied by <see cref="XdTheme"/>:
/// it puts it in the app resources with its variants (Xd.Accent, Xd.AccentHover, Xd.AccentSoft, Xd.AccentTile...,
/// see <see cref="XdKeys"/>), adapted to the theme, used by the views, the styles and the Xylocopadream.UI controls.
/// </summary>
public class MauiAccentColorService : IAccentColorService
{
    private const string AccentKey = "accent_color";

    public string DefaultAccent => XdColorMath.ToHex(XdTheme.DefaultAccent);

    public string GetAccent()
        => Preferences.Default.Get(AccentKey, DefaultAccent);

    public void SetAccent(string hex)
    {
        Preferences.Default.Set(AccentKey, hex);
        XdTheme.SetAccent(Color.FromArgb(hex));
    }

    /// <summary>
    /// Applies the remembered color at start; the colors follow the theme by themselves afterwards.
    /// </summary>
    public void Apply()
    {
        if (Microsoft.Maui.Controls.Application.Current is { } app)
            XdTheme.Apply(app, Color.FromArgb(GetAccent()));
    }
}
