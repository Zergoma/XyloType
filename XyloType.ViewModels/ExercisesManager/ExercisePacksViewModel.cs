using System.Collections.ObjectModel;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using XyloType.Application;
using XyloType.Application.DTOs;
using XyloType.Application.Interfaces;
using XyloType.Application.Models;

namespace XyloType.ViewModels.ExercisesManager;

/// <summary>
/// Exercise packs ready to use, from the first keys to whole texts, downloaded from the GitHub repository
/// and added to the exercises of the keyboard, in their own sections.
/// </summary>
public partial class ExercisePacksViewModel : ObservableObject
{
    private readonly IExercisePackSource _source;
    private readonly IExercisePackImporter _importer;
    private List<ExercisePackInfo> _catalog = [];

    public ExercisePacksViewModel(
        IExercisePackSource source,
        IExercisePackImporter importer,
        IKeyBoardLayoutAvailableService keyboardLayoutAvailableService,
        IUserKeyboardLayoutPreferenceService keyboardPreference)
    {
        _source = source;
        _importer = importer;

        KeyboardLayoutAvailable = keyboardLayoutAvailableService.GetKeyBoardAvailable();

        // the keyboard chosen on the home page
        Result<int> keyboard = keyboardPreference.GetKeyboardType();
        SelectedKeyboard = KeyboardLayoutAvailable.Find(k => keyboard.Success && (int)k.KeyBoardCode == keyboard.GetValue)
            ?? KeyboardLayoutAvailable.FirstOrDefault();
    }

    /// <summary>
    /// Asked before an import: the exercises being edited would be overwritten (false: no import).
    /// </summary>
    public Func<Task<bool>>? ConfirmBeforeImport { get; set; }

    /// <summary>
    /// Raised after a pack was imported: the exercises of the keyboard changed.
    /// </summary>
    public event EventHandler? Imported;

    /// <summary>
    /// The packs of the selected keyboard.
    /// </summary>
    public ObservableCollection<ExercisePackItem> Packs { get; } = [];

    public List<KeyBoardLayoutDto> KeyboardLayoutAvailable { get; }

    [ObservableProperty]
    public partial KeyBoardLayoutDto? SelectedKeyboard { get; set; }

    partial void OnSelectedKeyboardChanged(KeyBoardLayoutDto? value) => _ = ShowPacksAsync();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasPacks))]
    [NotifyPropertyChangedFor(nameof(HasNoPackForKeyboard))]
    public partial bool IsLoading { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasPacks))]
    [NotifyPropertyChangedFor(nameof(HasNoPackForKeyboard))]
    public partial bool IsLoaded { get; set; }

    public bool HasPacks => !IsLoading && Packs.Count > 0;

    public bool HasNoPackForKeyboard => IsLoaded && !IsLoading && Packs.Count == 0;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasLoadError))]
    public partial string LoadError { get; set; } = string.Empty;

    public bool HasLoadError => LoadError.Length > 0;

    [ObservableProperty]
    public partial string ErrorImport { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string SuccessImport { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotImporting))]
    public partial bool IsImporting { get; set; }

    public bool IsNotImporting => !IsImporting;

    /// <summary>
    /// Loads the catalog once (again after a failure, e.g. no connection).
    /// </summary>
    [RelayCommand]
    public async Task LoadAsync()
    {
        if (IsLoaded || IsLoading)
            return;

        IsLoading = true;
        LoadError = string.Empty;
        try
        {
            Result<ExercisePackCatalog> catalog = await Task.Run(() => _source.GetCatalogAsync());
            if (!catalog.Success)
            {
                LoadError = catalog.Error;
                return;
            }

            _catalog = [.. catalog.GetValue.Packs];
            IsLoaded = true;
        }
        finally
        {
            IsLoading = false;
        }

        await ShowPacksAsync();
    }

    /// <summary>
    /// The packs of the selected keyboard, with what is already imported.
    /// </summary>
    private async Task ShowPacksAsync()
    {
        Packs.Clear();
        if (SelectedKeyboard is not KeyBoardLayoutDto keyboard)
            return;

        foreach (ExercisePackInfo pack in _catalog.Where(p => string.Equals(p.Layout, keyboard.KeyBoardCode.ToString(), StringComparison.OrdinalIgnoreCase)))
            Packs.Add(new ExercisePackItem(pack));

        await RefreshImportedAsync(keyboard);
        OnPropertyChanged(nameof(HasPacks));
        OnPropertyChanged(nameof(HasNoPackForKeyboard));
    }

    private async Task RefreshImportedAsync(KeyBoardLayoutDto keyboard)
    {
        IReadOnlyDictionary<string, string> versions = await _importer.GetImportedVersionsAsync(keyboard);
        foreach (ExercisePackItem pack in Packs)
            pack.ImportedVersion = versions.GetValueOrDefault(pack.Info.Id);
    }

    [RelayCommand]
    public async Task ImportPack(ExercisePackItem pack)
    {
        ErrorImport = string.Empty;
        SuccessImport = string.Empty;

        if (SelectedKeyboard is not KeyBoardLayoutDto keyboard)
        {
            ErrorImport = "Choisissez un clavier";
            return;
        }

        if (ConfirmBeforeImport is not null && !await ConfirmBeforeImport())
            return;

        IsImporting = true;
        try
        {
            Result<ExercisePack> downloaded = await Task.Run(() => _source.DownloadAsync(pack.Info));
            if (!downloaded.Success)
            {
                ErrorImport = downloaded.Error;
                return;
            }

            Result<ExercisePackImportSummary> result = await _importer.ImportAsync(downloaded.GetValue, keyboard);
            if (!result.Success)
            {
                ErrorImport = result.Error;
                return;
            }

            ExercisePackImportSummary summary = result.GetValue;
            SuccessImport = summary.Updated == 0
                ? $"« {pack.Title} » importé : {summary.Added} exercices, à retrouver sur l'accueil."
                : $"« {pack.Title} » mis à jour : {summary.Added} nouveaux exercices, {summary.Updated} mis à jour.";

            await RefreshImportedAsync(keyboard);
            Imported?.Invoke(this, EventArgs.Empty);
        }
        finally
        {
            IsImporting = false;
        }
    }
}
