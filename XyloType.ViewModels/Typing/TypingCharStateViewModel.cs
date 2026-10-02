using CommunityToolkit.Mvvm.ComponentModel;

using XyloType.Domain.Typing;
using XyloType.Application.Models.Typing.Themes;
using XyloType.Application.Interfaces.Typing;

namespace XyloType.ViewModels.Typing;


public partial class TypingCharStateViewModel : ObservableObject
{
    private readonly ITypingTheme _typingTheme;
    public TypingChar Model { get; }
    public char Character => Model.Character;
    public List<char> Errors => Model.Errors;
    public TimeSpan ResponseTime => Model.RespondeTime;

    // private to restrain domain access
    private TypingCharState State => Model.State;

    private TypingStyle Style => _typingTheme.GetStyle(State);


    public TypingCharStateViewModel(
        ITypingTheme typingTheme,
        TypingChar model)
    {
        _typingTheme = typingTheme;
        Model = model;

        // All the magic is here
        // model state changed -> trigger property bound to UI
        Model.StateChanged += OnStateChanged;
    }

    private void OnStateChanged()
    {
        OnPropertyChanged(nameof(State));
        OnPropertyChanged(nameof(TextColor));
        OnPropertyChanged(nameof(BgColor));
        OnPropertyChanged(nameof(BorderColor));
        OnPropertyChanged(nameof(BorderThikness));
    }


    /// <summary>
    /// The piece of music changes (or starts again) on this letter.
    /// </summary>
    [ObservableProperty]
    public partial bool IsScoreChange { get; set; }

    public string TextColor => Style.TextColor;
    public string BgColor => Style.BackgroundColor;
    public string BorderColor => Style.BorderColor;
    public int BorderThikness => Style.BorderThickness;

}
