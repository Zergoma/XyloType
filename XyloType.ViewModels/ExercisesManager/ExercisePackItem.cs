using CommunityToolkit.Mvvm.ComponentModel;

using XyloType.Application.Models;

namespace XyloType.ViewModels.ExercisesManager;

/// <summary>
/// An exercise pack of the catalog, as shown in the exercises section.
/// </summary>
public partial class ExercisePackItem : ObservableObject
{
    public ExercisePackItem(ExercisePackInfo info)
    {
        Info = info;
    }

    public ExercisePackInfo Info { get; }

    public string Title => Info.Title;

    public string Level => Info.Level;

    public string Description => Info.Description;

    public string Details => $"{Info.ExerciseCount} exercices · version {Info.Version}";

    /// <summary>
    /// Version of the pack among the exercises of the keyboard, null if not imported.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsImported))]
    [NotifyPropertyChangedFor(nameof(HasUpdate))]
    [NotifyPropertyChangedFor(nameof(StateText))]
    [NotifyPropertyChangedFor(nameof(ActionText))]
    public partial string? ImportedVersion { get; set; }

    public bool IsImported => ImportedVersion is not null;

    public bool HasUpdate => IsImported && string.CompareOrdinal(ImportedVersion, Info.Version) < 0;

    public string StateText => HasUpdate ? "Mise à jour disponible" : IsImported ? "Déjà importé" : string.Empty;

    public string ActionText => HasUpdate ? "Mettre à jour" : IsImported ? "Réimporter" : "Importer";
}
