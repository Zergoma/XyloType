using System.Diagnostics;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using XyloType.Application;
using XyloType.Application.DTOs;
using XyloType.Application.Interfaces;
using XyloType.Application.Models;

namespace XyloType.ViewModels.Import;


public partial class ImportWordViewModel : ObservableObject
{
    private readonly IChoosePath _choosePathPresenter;
    private readonly IWordImportOrchestrator _wordImportOrchestrator;
    private readonly IKeyboardKeyLocatorManager _keyboardKeyLocatorManager;
    private readonly ILanguageAvailableService _languageAvailableService;
    private readonly IKeyBoardLayoutAvailableService _keyboardLayoutAvailableService;

    public ImportWordViewModel(
        IChoosePath choosePathPresnter,
        IWordImportOrchestrator wordImportService,
        IKeyboardKeyLocatorManager keyboardKeyLocatorManager,
        ILanguageAvailableService languageAvailableService,
        IKeyBoardLayoutAvailableService keyboardLayoutAvailableService)
    {
        _choosePathPresenter = choosePathPresnter;
        _wordImportOrchestrator = wordImportService;
        _keyboardKeyLocatorManager = keyboardKeyLocatorManager;
        _languageAvailableService = languageAvailableService;
        _keyboardLayoutAvailableService = keyboardLayoutAvailableService;
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

    [RelayCommand]
    public async Task ImportWordsFromFile()
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
                // created on the UI thread: reports come back on it
                Stopwatch stopwatch = Stopwatch.StartNew();
                Progress<WordImportProgress> progress = new(p => ReportProgress(p, stopwatch.Elapsed));
                string path = ImportFilePath;

                // reading and analyzing a big file must not freeze the UI
                Result<WordImportSummary> resuImport =
                    await Task.Run(() => _wordImportOrchestrator.ImportAsync(path, language, keyBoardLocator, progress));

                if (resuImport.Success)
                {
                    WordImportSummary summary = resuImport.GetValue;
                    SuccessImport =
                        $"Import terminé : {summary.WordsRead:N0} mots lus, " +
                        $"{summary.NewWords:N0} nouveaux, {summary.UpdatedWords:N0} déjà connus, " +
                        $"{summary.IgnoredWords:N0} ignorés (impossibles à taper sur ce clavier).";
                }
                else
                {
                    ErrorImport = resuImport.Error;
                }
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

    private void ReportProgress(WordImportProgress progress, TimeSpan elapsed)
    {
        ProgressValue = progress.Fraction;

        string text = $"{progress.Fraction:P0} · {progress.WordsRead:N0} mots lus";

        // estimate only once the speed is meaningful
        if (progress.Fraction is >= 0.02 and < 1 && elapsed > TimeSpan.FromSeconds(1))
        {
            TimeSpan remaining = elapsed / progress.Fraction * (1 - progress.Fraction);
            text += $" · {FormatRemaining(remaining)}";
        }
        else if (progress.Fraction >= 1)
        {
            text += " · enregistrement…";
        }

        ProgressText = text;
    }

    private static string FormatRemaining(TimeSpan remaining)
        => remaining.TotalSeconds < 60
            ? $"environ {Math.Max(1, (int)Math.Ceiling(remaining.TotalSeconds))} s restantes"
            : $"environ {(int)Math.Ceiling(remaining.TotalMinutes)} min restantes";
}
