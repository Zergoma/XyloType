using System.Collections.ObjectModel;

using CommunityToolkit.Mvvm.ComponentModel;

using XyloType.Domain.Typing;
using XyloType.Application.Interfaces.Typing;

namespace XyloType.ViewModels.Typing;

public partial class TypingLineStateViewModel : ObservableObject
{
    public TypingLine Model { get; }
    private readonly ITypingTheme _theme;
    public ObservableCollection<TypingCharStateViewModel> Characters { get; } = [];

    public TypingLineStateViewModel(
        ITypingTheme theme,
        TypingLine model)
    {
        _theme = theme;
        Model = model;

        Build();
    }

    private void Build()
    {
        Characters.Clear();

        foreach (TypingChar c in Model.Characters)
        {
            Characters.Add(new TypingCharStateViewModel(_theme, c));
        }
    }
}
