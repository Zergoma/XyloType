using CommunityToolkit.Mvvm.ComponentModel;

using XyloType.Application.Models.Typing.Exercices;

namespace XyloType.ViewModels.ExercisesManager;

/// <summary>
/// One exercise in the manager list, a live view on the exercise being edited.
/// </summary>
public partial class ExerciseListItemViewModel : ObservableObject
{
    public TypingExercise Exercise { get; }

    public ExerciseListItemViewModel(TypingExercise exercise)
    {
        Exercise = exercise;
    }

    public Guid Id => Exercise.Id;

    public string Name
        => string.IsNullOrWhiteSpace(Exercise.Name) ? "(sans nom)" : Exercise.Name;

    public string Letters => Exercise.AllowedCharacters;

    public string TypeText
        => Exercise.TextDataType is TypingTextDataDynamic ? "Dynamique" : "Statique";

    [ObservableProperty]
    public partial bool IsSelected { get; set; }

    /// <summary>
    /// Refreshes the displayed values after the exercise has been edited.
    /// </summary>
    public void Refresh()
    {
        OnPropertyChanged(nameof(Name));
        OnPropertyChanged(nameof(Letters));
        OnPropertyChanged(nameof(TypeText));
    }
}
