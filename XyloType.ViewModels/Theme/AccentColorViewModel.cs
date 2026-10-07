using CommunityToolkit.Mvvm.ComponentModel;

using XyloType.Application.Interfaces;

namespace XyloType.ViewModels.Theme;

/// <summary>
/// A color proposed in the picker.
/// </summary>
public record AccentPreset(string Name, string Hex);

/// <summary>
/// Main color of the app, chosen in the color picker (presets, shade, hex code): every change is applied at once
/// (and remembered).
/// </summary>
public partial class AccentColorViewModel : ObservableObject
{
    private readonly IAccentColorService _accentService;

    public AccentColorViewModel(IAccentColorService accentService)
    {
        _accentService = accentService;
        Accent = accentService.GetAccent();
    }

    public IReadOnlyList<AccentPreset> Presets { get; } =
    [
        new("Bleu", "#2F6FEB"),
        new("Indigo", "#4F46E5"),
        new("Violet", "#7C3AED"),
        new("Pourpre", "#A21CAF"),
        new("Rose", "#DB2777"),
        new("Rouge", "#DC2626"),
        new("Orange", "#EA580C"),
        new("Ambre", "#D97706"),
        new("Vert", "#16A34A"),
        new("Émeraude", "#059669"),
        new("Sarcelle", "#0D9488"),
        new("Cyan", "#0891B2"),
    ];

    /// <summary>
    /// The color applied, "#RRGGBB".
    /// </summary>
    [ObservableProperty]
    public partial string Accent { get; set; }

    /// <summary>
    /// The color of the app before any choice.
    /// </summary>
    public string DefaultAccent => _accentService.DefaultAccent;

    partial void OnAccentChanged(string value)
    {
        if (!string.Equals(value, _accentService.GetAccent(), StringComparison.OrdinalIgnoreCase))
            _accentService.SetAccent(value);
    }
}
