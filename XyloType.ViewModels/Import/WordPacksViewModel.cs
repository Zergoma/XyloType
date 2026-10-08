using System.Collections.ObjectModel;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using XyloType.Application;
using XyloType.Application.DTOs;
using XyloType.Application.Interfaces;
using XyloType.Application.Models;
using XyloType.Domain.Entities;

namespace XyloType.ViewModels.Import;

/// <summary>
/// A word pack of the catalog, as shown in the import section.
/// </summary>
public partial class WordPackItem : ObservableObject
{
    public WordPackItem(WordPackInfo info)
    {
        Info = info;
    }

    public WordPackInfo Info { get; }

    public string Title => Info.Title;

    public string Details => $"{Info.WordCount:N0} mots · {Info.Size / 1024.0:N0} Ko · version {Info.Version}";

    public string SourcesText => Info.Sources.Count == 0 ? string.Empty : "Tiré de : " + string.Join(", ", Info.Sources);

    /// <summary>
    /// This very pack (same checksum) is already in the import history.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ActionText))]
    public partial bool IsImported { get; set; }

    public string ActionText => IsImported ? "Réimporter" : "Télécharger et importer";
}

/// <summary>
/// Word packs ready to use, downloaded from the GitHub repository and imported like a text:
/// the app is ready at once, without importing books.
/// </summary>
public partial class WordPacksViewModel : ObservableObject
{
    private readonly IWordPackSource _packSource;
    private readonly IWordImportOrchestrator _importOrchestrator;
    private readonly IKeyboardKeyLocatorManager _keyboardKeyLocatorManager;
    private readonly IImportedSourceRepository _sourceRepository;
    private readonly IDactyloRepository _wordRepository;
    private bool _isLoaded;

    public WordPacksViewModel(
        IWordPackSource packSource,
        IWordImportOrchestrator importOrchestrator,
        IKeyboardKeyLocatorManager keyboardKeyLocatorManager,
        IKeyBoardLayoutAvailableService keyboardLayoutAvailableService,
        IUserKeyboardLayoutPreferenceService keyboardPreference,
        IImportedSourceRepository sourceRepository,
        IDactyloRepository wordRepository)
    {
        _packSource = packSource;
        _importOrchestrator = importOrchestrator;
        _keyboardKeyLocatorManager = keyboardKeyLocatorManager;
        _sourceRepository = sourceRepository;
        _wordRepository = wordRepository;

        KeyboardLayoutAvailable = keyboardLayoutAvailableService.GetKeyBoardAvailable();

        // the keyboard chosen on the home page
        Result<int> keyboard = keyboardPreference.GetKeyboardType();
        SelectedKeyboard = KeyboardLayoutAvailable.Find(k => keyboard.Success && (int)k.KeyBoardCode == keyboard.GetValue)
            ?? KeyboardLayoutAvailable.FirstOrDefault();
    }

    /// <summary>
    /// Raised after a pack was imported: the history and the word counts change.
    /// </summary>
    public event EventHandler? Imported;

    public ObservableCollection<WordPackItem> Packs { get; } = [];

    public List<KeyBoardLayoutDto> KeyboardLayoutAvailable { get; }

    [ObservableProperty]
    public partial KeyBoardLayoutDto? SelectedKeyboard { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasPacks))]
    public partial bool IsLoading { get; set; }

    public bool HasPacks => !IsLoading && Packs.Count > 0;

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

    [ObservableProperty]
    public partial string ProgressText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial double ProgressValue { get; set; }

    /// <summary>
    /// The database has no word yet (first start): the packs are worth offering.
    /// </summary>
    public async Task<bool> HasNoWordsAsync()
    {
        WordSearchPage page = await _wordRepository.SearchPageAsync(
            new WordQueryBuilder().WithExclusion(WordExclusionFilter.All).Build(), WordSort.Default, 0, 1);
        return page.TotalCount == 0;
    }

    /// <summary>
    /// Loads the catalog once (again after a failure, e.g. no connection).
    /// </summary>
    [RelayCommand]
    public async Task LoadAsync()
    {
        if (_isLoaded || IsLoading)
            return;

        IsLoading = true;
        LoadError = string.Empty;
        try
        {
            Result<WordPackCatalog> catalog = await Task.Run(() => _packSource.GetCatalogAsync());
            if (!catalog.Success)
            {
                LoadError = catalog.Error;
                return;
            }

            Packs.Clear();
            foreach (WordPackInfo pack in catalog.GetValue.Packs)
                Packs.Add(new WordPackItem(pack));

            await RefreshImportedAsync();
            _isLoaded = true;
        }
        finally
        {
            IsLoading = false;
        }
    }

    // the generated ImportPackCancelCommand stops the download or the import: nothing is written then
    [RelayCommand(IncludeCancelCommand = true)]
    public async Task ImportPack(WordPackItem pack, CancellationToken cancellationToken)
    {
        ErrorImport = string.Empty;
        SuccessImport = string.Empty;

        if (SelectedKeyboard is null)
        {
            ErrorImport = "Choisissez un clavier";
            return;
        }

        Result<IKeyboardKeysLocator> keyboard = _keyboardKeyLocatorManager.GetKeyBoardKeyLocator(SelectedKeyboard);
        if (!keyboard.Success)
        {
            ErrorImport = keyboard.Error;
            return;
        }

        IsImporting = true;
        ProgressValue = 0;
        ProgressText = $"Téléchargement de « {pack.Title} »…";
        string? file = null;
        try
        {
            // created on the UI thread: reports come back on it
            Progress<double> download = new(fraction =>
            {
                ProgressValue = fraction;
                ProgressText = $"Téléchargement {fraction:P0}";
            });

            Result<string> downloaded = await Task.Run(() => _packSource.DownloadAsync(pack.Info, download, cancellationToken), cancellationToken);
            if (!downloaded.Success)
            {
                ErrorImport = downloaded.Error;
                return;
            }

            file = downloaded.GetValue;
            Progress<WordImportProgress> import = new(progress =>
            {
                ProgressValue = progress.Fraction;
                ProgressText = progress.Phase == WordImportPhase.Reading
                    ? $"Analyse {progress.Fraction:P0} · {progress.WordsRead:N0} mots"
                    : $"Enregistrement {progress.Fraction:P0}";
            });

            IKeyboardKeysLocator layout = keyboard.GetValue;
            Result<WordImportSummary> result = await Task.Run(
                () => _importOrchestrator.ImportPackAsync(downloaded.GetValue, pack.Info, layout, import, cancellationToken),
                cancellationToken);

            if (!result.Success)
            {
                ErrorImport = result.Error;
                return;
            }

            WordImportSummary summary = result.GetValue;
            SuccessImport =
                $"« {pack.Title} » importé : {summary.NewWords:N0} nouveaux mots, {summary.UpdatedWords:N0} déjà connus, " +
                $"{summary.IgnoredWords:N0} impossibles à taper sur ce clavier.";

            await RefreshImportedAsync();
            Imported?.Invoke(this, EventArgs.Empty);
        }
        catch (OperationCanceledException)
        {
            ErrorImport = "Import annulé : rien n'a été ajouté à la base.";
        }
        finally
        {
            if (file is not null)
                TryDelete(file);

            IsImporting = false;
            ProgressText = string.Empty;
            ProgressValue = 0;
        }
    }

    private async Task RefreshImportedAsync()
    {
        HashSet<string> hashes = [.. (await _sourceRepository.GetAllAsync()).Select(s => s.ContentHash)];
        foreach (WordPackItem pack in Packs)
            pack.IsImported = hashes.Contains(pack.Info.Sha256);

        OnPropertyChanged(nameof(HasPacks));
    }

    private static void TryDelete(string path)
    {
        try { File.Delete(path); } catch (IOException) { } catch (UnauthorizedAccessException) { }
    }
}
