using CommunityToolkit.Mvvm.ComponentModel;

using XyloType.Domain.Music;

namespace XyloType.ViewModels.Typing;

/// <summary>
/// A piece of music in the settings, with its switch.
/// </summary>
public partial class ScoreOptionViewModel : ObservableObject
{
    private readonly Action<ScoreOptionViewModel> _onEnabledChanged;

    public ScoreOptionViewModel(Score score, bool isEnabled, Action<ScoreOptionViewModel> onEnabledChanged)
    {
        Score = score;
        _isEnabled = isEnabled;
        _onEnabledChanged = onEnabledChanged;
    }

    public Score Score { get; }

    public string Title => Score.Title;

    public string Composer => Score.Composer;

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
