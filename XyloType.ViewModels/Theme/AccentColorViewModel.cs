using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using XyloType.Application.Interfaces;

namespace XyloType.ViewModels.Theme;

/// <summary>
/// A color proposed in the picker.
/// </summary>
public record AccentPreset(string Name, string Hex);

/// <summary>
/// Choice of the main color of the app: among nice presets, by its hex code, or picked on the hue bar and the shade square.
/// Every change is applied at once (and remembered).
/// </summary>
public partial class AccentColorViewModel : ObservableObject
{
    private readonly IAccentColorService _accentService;
    private bool _isUpdating;

    public AccentColorViewModel(IAccentColorService accentService)
    {
        _accentService = accentService;
        SetColor(accentService.GetAccent(), apply: false);
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
    public partial string Accent { get; private set; } = string.Empty;

    /// <summary>
    /// The hex code typed by the user: applied as soon as it is a color.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsHexValid))]
    public partial string HexText { get; set; } = string.Empty;

    public bool IsHexValid => ColorMath.NormalizeHex(HexText) is not null;

    // position in the picker
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HueColor))]
    public partial double Hue { get; private set; }

    [ObservableProperty]
    public partial double Saturation { get; private set; }

    [ObservableProperty]
    public partial double Value { get; private set; }

    /// <summary>
    /// The pure color of the hue, the background of the shade square.
    /// </summary>
    public string HueColor => ColorMath.HsvToHex(Hue, 1, 1);

    partial void OnHexTextChanged(string value)
    {
        if (_isUpdating || ColorMath.NormalizeHex(value) is not string hex)
            return;

        SetColor(hex, apply: true, keepHexText: true);
    }

    [RelayCommand]
    public void SelectPreset(AccentPreset preset)
        => SetColor(preset.Hex, apply: true);

    [RelayCommand]
    public void ResetToDefault()
        => SetColor(_accentService.DefaultAccent, apply: true);

    /// <summary>
    /// Hue picked on the hue bar (0 to 360°), keeping the shade.
    /// </summary>
    public void PickHue(double hue)
        => SetHsv(Math.Clamp(hue, 0, 359.99), Saturation, Value);

    /// <summary>
    /// Shade picked on the square: saturation from left to right, brightness from bottom to top (0 to 1).
    /// </summary>
    public void PickShade(double saturation, double value)
        => SetHsv(Hue, Math.Clamp(saturation, 0, 1), Math.Clamp(value, 0, 1));

    private void SetHsv(double hue, double saturation, double value)
    {
        string hex = ColorMath.HsvToHex(hue, saturation, value);

        _isUpdating = true;
        Hue = hue;
        Saturation = saturation;
        Value = value;
        Accent = hex;
        HexText = hex;
        _isUpdating = false;

        _accentService.SetAccent(hex);
    }

    private void SetColor(string hex, bool apply, bool keepHexText = false)
    {
        var (hue, saturation, value) = ColorMath.HexToHsv(hex);

        _isUpdating = true;
        // a grey has no hue: keep the one of the bar
        if (saturation > 0)
            Hue = hue;
        Saturation = saturation;
        Value = value;
        Accent = hex;
        if (!keepHexText)
            HexText = hex;
        _isUpdating = false;

        if (apply)
            _accentService.SetAccent(hex);
    }
}
