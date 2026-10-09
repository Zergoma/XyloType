using CommunityToolkit.Mvvm.ComponentModel;

namespace XyloType.ViewModels.TypingLauncher;

/// <summary>
/// A section of exercises on the home page: its title, then its tiles.
/// </summary>
public partial class ExerciseGroupViewModel : ObservableObject
{
    public ExerciseGroupViewModel(string title, IReadOnlyList<ExerciceItemViewModel> items)
    {
        Title = title;
        Items = items;
    }

    public string Title { get; }

    public IReadOnlyList<ExerciceItemViewModel> Items { get; }

    public bool HasItems => Items.Count > 0;

    /// <summary>
    /// E.g. "3 / 8 faits".
    /// </summary>
    public string DoneText => $"{Items.Count(i => i.IsDone)} / {Items.Count} faits";

    public bool IsAllDone => HasItems && Items.All(i => i.IsDone);

    /// <summary>
    /// The results of the user changed.
    /// </summary>
    public void RefreshProgress()
    {
        OnPropertyChanged(nameof(DoneText));
        OnPropertyChanged(nameof(IsAllDone));
    }
}
