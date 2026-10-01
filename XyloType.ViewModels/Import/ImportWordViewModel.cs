using System.Collections.ObjectModel;
using System.Diagnostics;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using XyloType.Application;
using XyloType.Application.DTOs;
using XyloType.Application.Interfaces;
using XyloType.Application.Models;
using XyloType.Domain.Entities;

namespace XyloType.ViewModels.Import;


public partial class ImportWordViewModel : ObservableObject
{
    private readonly IChoosePath _choosePathPresenter;
    private readonly IWordImportOrchestrator _wordImportOrchestrator;
    private readonly IKeyboardKeyLocatorManager _keyboardKeyLocatorManager;
    private readonly ILanguageAvailableService _languageAvailableService;
    private readonly IKeyBoardLayoutAvailableService _keyboardLayoutAvailableService;
    private readonly IImportDuplicateChecker _duplicateChecker;
    private readonly IImportedSourceRepository _sourceRepository;
    private readonly IUserDialogService _dialogService;

    public ImportWordViewModel(
        IChoosePath choosePathPresnter,
        IWordImportOrchestrator wordImportService,
        IKeyboardKeyLocatorManager keyboardKeyLocatorManager,
        ILanguageAvailableService languageAvailableService,
        IKeyBoardLayoutAvailableService keyboardLayoutAvailableService,
        IImportDuplicateChecker duplicateChecker,
        IImportedSourceRepository sourceRepository,
        IUserDialogService dialogService)
    {
        _choosePathPresenter = choosePathPresnter;
        _wordImportOrchestrator = wordImportService;
        _keyboardKeyLocatorManager = keyboardKeyLocatorManager;
        _languageAvailableService = languageAvailableService;
        _keyboardLayoutAvailableService = keyboardLayoutAvailableService;
        _duplicateChecker = duplicateChecker;
        _sourceRepository = sourceRepository;
        _dialogService = dialogService;
    }


    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsFileSelected))]
    public partial string ImportFilePath { get; set; }

    public List<String> AllLanguage => _languageAvailableService.GetAvailableLanguage();

    public object? SelectedLanguage { get; set; } = null;

    public List<KeyBoardLayoutDto> KeyboardLayoutAvailable => [.. _keyboardLayoutAvailableService.GetKeyBoardAvailable()];
    public object? SelectedKeyboard { get; set; } = null;

    public bool IsFileSelected => !string.IsNullOrWhiteSpace(ImportFilePath) && File.Exists(ImportFilePath);

    [ObservableProperty]
    public partial string ErrorImport { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string SuccessImport { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotImporting))]
    public partial bool IsImporting { get; set; }

    public bool IsNotImporting => !IsImporting;

    [ObservableProperty]
    public partial string ProgressText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial double ProgressValue { get; set; }


    [RelayCommand]
    public async Task SelectFile()
    {
        Result<string?> resuPath =
            await _choosePathPresenter.SelectPathAsync();

        if (!resuPath.Success)
            return;

        if (resuPath.Value is null)
            return;

        string srcFile = resuPath.Value;

        if (!File.Exists(srcFile))
            return;

        ImportFilePath = srcFile;
    }

    // the generated ImportWordsFromFileCancelCommand stops the import: nothing is written then
    [RelayCommand(IncludeCancelCommand = true)]
    public async Task ImportWordsFromFile(CancellationToken cancellationToken)
    {
        ErrorImport = string.Empty;
        SuccessImport = string.Empty;

        if (SelectedLanguage is null)
        {
            ErrorImport = "Choisissez une langue";
            return;
        }

        if (SelectedKeyboard is null)
        {
            ErrorImport = "Choisissez un clavier";
            return;
        }


        if (SelectedLanguage is string language &&
            SelectedKeyboard is KeyBoardLayoutDto keyboard)
        {
            Result<IKeyboardKeysLocator> keyBoardLocatorResult =
                _keyboardKeyLocatorManager.GetKeyBoardKeyLocator(keyboard);

            if (!keyBoardLocatorResult.Success)
            {
                ErrorImport = keyBoardLocatorResult.Error;
                return;
            }

            IKeyboardKeysLocator keyBoardLocator = keyBoardLocatorResult.GetValue;

            IsImporting = true;
            ProgressValue = 0;
            ProgressText = "Préparation…";
            try
            {
                string fileToCheck = ImportFilePath;
                ProgressText = "Vérification de l'historique…";
                ImportCheckResult check = await Task.Run(() => _duplicateChecker.CheckAsync(fileToCheck, cancellationToken), cancellationToken);

                if (!await ConfirmImportAsync(check))
                {
                    ErrorImport = "Import annulé";
                    return;
                }

                ProgressText = "Préparation…";

                // created on the UI thread: reports come back on it
                _phaseStopwatch = Stopwatch.StartNew();
                _currentPhase = WordImportPhase.Reading;
                Progress<WordImportProgress> progress = new(ReportProgress);
                string path = ImportFilePath;

                // reading and analyzing a big file must not freeze the UI
                Result<WordImportSummary> resuImport =
                    await Task.Run(() => _wordImportOrchestrator.ImportAsync(path, language, keyBoardLocator, progress, cancellationToken), cancellationToken);

                if (resuImport.Success)
                {
                    WordImportSummary summary = resuImport.GetValue;
                    SuccessImport =
                        $"Import terminé : {summary.WordsRead:N0} mots lus, " +
                        $"{summary.NewWords:N0} nouveaux, {summary.UpdatedWords:N0} déjà connus, " +
                        $"{summary.IgnoredWords:N0} ignorés (impossibles à taper sur ce clavier)." +
                        FormatIgnored(summary);

                    await LoadHistoryAsync();
                }
                else
                {
                    ErrorImport = resuImport.Error;
                }
            }
            catch (OperationCanceledException)
            {
                // cancelled before the import started (history check)
                ErrorImport = "Import annulé : rien n'a été ajouté à la base.";
            }
            finally
            {
                IsImporting = false;
                ProgressText = string.Empty;
                ProgressValue = 0;
            }
        }
        else
        {
            ErrorImport = "Select a language first";
        }
    }


    /// <summary>
    /// Previous imports, most recent first.
    /// </summary>
    public ObservableCollection<ImportedSourceItem> History { get; } = [];

    public bool HasHistory => History.Count > 0;

    public async Task LoadHistoryAsync()
    {
        List<ImportedSource> sources = await _sourceRepository.GetAllAsync();

        History.Clear();
        foreach (ImportedSource source in sources)
            History.Add(new ImportedSourceItem(source));

        OnPropertyChanged(nameof(HasHistory));
    }

    /// <summary>
    /// Asks the user before importing a text already imported or with a close title.
    /// </summary>
    private async Task<bool> ConfirmImportAsync(ImportCheckResult check)
    {
        if (check.SameContent is ImportedSource same)
        {
            return await _dialogService.ConfirmAsync(
                "Texte déjà importé",
                $"Ce texte a déjà été importé le {same.ImportedAtUtc.ToLocalTime():d} sous le titre « {same.Title} » " +
                $"({same.WordsRead:N0} mots).\n\nLe réimporter ajouterait une deuxième fois ses occurrences. Importer quand même ?",
                "Importer quand même",
                "Ne pas importer");
        }

        if (check.CloseTitles.Count > 0)
        {
            string titles = string.Join("\n", check.CloseTitles.Select(s => $"• {s.Title} ({s.ImportedAtUtc.ToLocalTime():d})"));
            return await _dialogService.ConfirmAsync(
                "Titre proche déjà importé",
                $"« {check.Title} » ressemble à :\n{titles}\n\nC'est peut-être le même livre (autre édition, copie). Importer quand même ?",
                "Importer",
                "Ne pas importer");
        }

        return true;
    }
    /// <summary>
    /// The ignored words, so the user sees why they were left out (œ, ß... are not on the keyboard).
    /// </summary>
    private static string FormatIgnored(WordImportSummary summary)
    {
        if (summary.IgnoredSample.Count == 0)
            return string.Empty;

        string more = summary.IgnoredWords > summary.IgnoredSample.Count ? "…" : string.Empty;
        return Environment.NewLine + $"Ignorés : {string.Join(", ", summary.IgnoredSample)}{more}";
    }

    private Stopwatch _phaseStopwatch = new();
    private WordImportPhase _currentPhase;

    private void ReportProgress(WordImportProgress progress)
    {
        // each step has its own bar and its own remaining time
        if (progress.Phase != _currentPhase)
        {
            _currentPhase = progress.Phase;
            _phaseStopwatch.Restart();
        }

        ProgressValue = progress.Fraction;

        string text = progress.Phase == WordImportPhase.Reading
            ? $"Lecture {progress.Fraction:P0} · {progress.WordsRead:N0} mots lus"
            : $"Enregistrement {progress.Fraction:P0}";

        // estimate only once the speed is meaningful
        TimeSpan elapsed = _phaseStopwatch.Elapsed;
        if (progress.Fraction is >= 0.02 and < 1 && elapsed > TimeSpan.FromSeconds(1))
        {
            TimeSpan remaining = elapsed / progress.Fraction * (1 - progress.Fraction);
            text += $" · {FormatRemaining(remaining)}";
        }

        ProgressText = text;
    }

    private static string FormatRemaining(TimeSpan remaining)
        => remaining.TotalSeconds < 60
            ? $"environ {Math.Max(1, (int)Math.Ceiling(remaining.TotalSeconds))} s restantes"
            : $"environ {(int)Math.Ceiling(remaining.TotalMinutes)} min restantes";
}
