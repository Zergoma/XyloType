using System.Collections.ObjectModel;

using CommunityToolkit.Mvvm.ComponentModel;

using XyloType.Application.Models;
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
    /// The names of the levels, for the picker.
    /// </summary>
    public static IReadOnlyList<string> LevelNames { get; } = [.. TypingLevelNames.All.Select(TypingLevelNames.Of)];

    /// <summary>
    /// The level of the exercises of the section: their results are judged against it.
    /// </summary>
    public string LevelName
    {
        get => TypingLevelNames.Of(Section.Level);
        set
        {
            if (value is null || value == LevelName)
                return;

            Section.Level = TypingLevelNames.Parse(value);
            OnPropertyChanged();
            LevelChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>
    /// The level was changed in the editor.
    /// </summary>
    public event EventHandler? LevelChanged;

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
