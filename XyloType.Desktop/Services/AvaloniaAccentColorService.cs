using Avalonia.Media;

using Xylocopadream.UI.Avalonia;

using XyloType.Application.Interfaces;

namespace XyloType.Desktop.Services;

/// <summary>
/// The main color chosen by the user, applied by <see cref="XdAccent"/> (its variants for both themes, used by the
/// views and the Xylocopadream.UI controls) and remembered in the settings, whoever changes it
/// (the color button of the right stripe applies it directly). Must be registered as a singleton.
/// </summary>
public sealed class AvaloniaAccentColorService : IAccentColorService
{
    private const string AccentKey = "accent_color";

    private readonly ISettingsStore _store;

    public AvaloniaAccentColorService(ISettingsStore store)
    {
        _store = store;
        XdAccent.Changed += (_, _) =>
        {
            // XdAccent writes the colors of the themes again: the variants of the app go back on top
            AccentVariants.Apply();
            _store.Set(AccentKey, Hex(XdAccent.Current));
        };
    }

    /// <summary>
    /// The color of the theme, the one the color picker goes back to.
    /// </summary>
    public string DefaultAccent => Hex(XdAccent.Default);

    public string GetAccent() => _store.Get(AccentKey, DefaultAccent);

    public void SetAccent(string hex)
    {
        if (Color.TryParse(hex, out Color color))
            XdAccent.Apply(color);
    }

    /// <summary>
    /// Applies the remembered color at start.
    /// </summary>
    public void Apply()
        => XdAccent.Apply(Color.TryParse(GetAccent(), out Color color) ? color : XdAccent.Default);

    private static string Hex(Color color) => $"#{color.R:X2}{color.G:X2}{color.B:X2}";
}
