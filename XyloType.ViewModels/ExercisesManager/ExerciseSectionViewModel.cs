using System.Collections.ObjectModel;

using CommunityToolkit.Mvvm.ComponentModel;

using XyloType.Application.Models.Typing.Exercices;

namespace XyloType.ViewModels.ExercisesManager;

/// <summary>
/// A section in the manager list: its title, then its exercises (reordered by drag and drop inside it).
/// </summary>
public partial class ExerciseSectionViewModel : ObservableObject
{
    public ExerciseSectionViewModel(ExerciseSection section)
    {
        Section = section;
    }

    public ExerciseSection Section { get; }

    public Guid Id => Section.Id;

    public string Title => Section.Title;

    /// <summary>
    /// Sections imported from a pack: an update of the pack overwrites their exercises.
    /// </summary>
    public bool IsFromPack => Section.PackId is not null;

    public ObservableCollection<ExerciseListItemViewModel> Items { get; } = [];

    public bool IsEmpty => Items.Count == 0;

    [ObservableProperty]
    public partial bool IsFirst { get; set; }

    [ObservableProperty]
    public partial bool IsLast { get; set; }

    public void Refresh()
    {
        OnPropertyChanged(nameof(Title));
        OnPropertyChanged(nameof(IsEmpty));
    }

    /// <summary>
    /// Shown in the section picker of the editor.
    /// </summary>
    public override string ToString() => Title;
}
