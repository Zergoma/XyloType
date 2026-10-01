using CommunityToolkit.Mvvm.ComponentModel;

using XyloType.Application.Models;

namespace XyloType.ViewModels.Typing;

/// <summary>
/// An instrument in the settings, with its switch for the random pick.
/// </summary>
public partial class InstrumentOptionViewModel : ObservableObject
{
    private readonly Action<InstrumentOptionViewModel> _onEnabledChanged;

    public InstrumentOptionViewModel(
        InstrumentChoice value,
        string label,
        bool isEnabled,
        Action<InstrumentOptionViewModel> onEnabledChanged)
    {
        Value = value;
        Label = label;
        _isEnabled = isEnabled;
        _onEnabledChanged = onEnabledChanged;
    }

    public InstrumentChoice Value { get; }

    public string Label { get; }

    private bool _isEnabled;
    public bool IsEnabled
    {
        get => _isEnabled;
        set
        {
            if (SetProperty(ref _isEnabled, value))
                _onEnabledChanged(this);
        }
    }
}
