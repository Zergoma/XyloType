using System.Collections.ObjectModel;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using XyloType.Application;
using XyloType.Application.DTOs;
using XyloType.Application.Interfaces;
using XyloType.Application.Mappers;
using XyloType.Application.Models;
using XyloType.Domain.Enums;

namespace XyloType.ViewModels.WordsExplorer;

/// <summary>
/// Words menu: search the imported words with criteria, exclude or restore them.
/// </summary>
public partial class WordsExplorerViewModel : ObservableObject
{
    private const int PageSize = 100;
    private static readonly TimeSpan s_searchDelay = TimeSpan.FromMilliseconds(350);

    private readonly IDactyloRepository _repository;
    private readonly IUserKeyboardLayoutPreferenceService _keyboardPreference;
    private readonly IKeyBoardLayoutAvailableService _keyboardLayoutAvailableService;

    private KeyboardLayout? _layout;
    private WordSearchCriteria _currentCriteria;
    private WordSort _currentSort = WordSort.Default;
    private CancellationTokenSource? _pendingSearch;
    private bool _isInitialized;

    public WordsExplorerViewModel(
        IDactyloRepository repository,
        IUserKeyboardLayoutPreferenceService keyboardPreference,
        IKeyBoardLayoutAvailableService keyboardLayoutAvailableService,
        ILanguageAvailableService languageAvailableService)
    {
        _repository = repository;
        _keyboardPreference = keyboardPreference;
        _keyboardLayoutAvailableService = keyboardLayoutAvailableService;

        LanguageOptions =
            [new("Toutes", null), .. languageAvailableService.GetAvailableLanguage().Select(l => new PickerOption<string?>(l, l))];
        LanguageSelected = LanguageOptions[0];
        HandsSelected = HandsOptions[0];
        ExclusionSelected = ExclusionOptions[0];
    }

    #region Filters

    public IReadOnlyList<PickerOption<string?>> LanguageOptions { get; }

    public IReadOnlyList<PickerOption<HandFilter>> HandsOptions { get; } =
    [
        new("Toutes", HandFilter.Any),
        new("Main gauche seule", HandFilter.LeftOnly),
        new("Main droite seule", HandFilter.RightOnly),
        new("Deux mains", HandFilter.BothHands),
    ];

    public IReadOnlyList<PickerOption<WordExclusionFilter>> ExclusionOptions { get; } =
    [
        new("Mots actifs", WordExclusionFilter.ActiveOnly),
        new("Mots exclus", WordExclusionFilter.ExcludedOnly),
        new("Tous", WordExclusionFilter.All),
    ];

    [ObservableProperty] public partial string SearchText { get; set; } = string.Empty;
    [ObservableProperty] public partial string OnlyLetters { get; set; } = string.Empty;
    [ObservableProperty] public partial string MinLength { get; set; } = string.Empty;
    [ObservableProperty] public partial string MaxLength { get; set; } = string.Empty;
    [ObservableProperty] public partial string MinOccurrences { get; set; } = string.Empty;
    [ObservableProperty] public partial string MaxOccurrences { get; set; } = string.Empty;
    [ObservableProperty] public partial PickerOption<string?> LanguageSelected { get; set; }
    [ObservableProperty] public partial PickerOption<HandFilter> HandsSelected { get; set; }
    [ObservableProperty] public partial PickerOption<WordExclusionFilter> ExclusionSelected { get; set; }

    // every filter change runs the search (after a short delay while typing)
    partial void OnSearchTextChanged(string value) => ScheduleSearch();
    partial void OnOnlyLettersChanged(string value) => ScheduleSearch();
    partial void OnMinLengthChanged(string value) => ScheduleSearch();
    partial void OnMaxLengthChanged(string value) => ScheduleSearch();
    partial void OnMinOccurrencesChanged(string value) => ScheduleSearch();
    partial void OnMaxOccurrencesChanged(string value) => ScheduleSearch();
    partial void OnLanguageSelectedChanged(PickerOption<string?> value) => OnColumnFilterChanged();
    partial void OnHandsSelectedChanged(PickerOption<HandFilter> value) => OnColumnFilterChanged();
    partial void OnExclusionSelectedChanged(PickerOption<WordExclusionFilter> value) => OnColumnFilterChanged();

    [RelayCommand]
    public void ResetFilters()
    {
        SearchText = string.Empty;
        OnlyLetters = string.Empty;
        MinLength = string.Empty;
        MaxLength = string.Empty;
        MinOccurrences = string.Empty;
        MaxOccurrences = string.Empty;
        LanguageSelected = LanguageOptions[0];
        HandsSelected = HandsOptions[0];
        ExclusionSelected = ExclusionOptions[0];
    }

    #endregion


    #region Sort (click on the column headers)

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TextHeader))]
    [NotifyPropertyChangedFor(nameof(OccurrencesHeader))]
    [NotifyPropertyChangedFor(nameof(LengthHeader))]
    [NotifyPropertyChangedFor(nameof(HandsHeader))]
    [NotifyPropertyChangedFor(nameof(LanguageHeader))]
    [NotifyPropertyChangedFor(nameof(ExcludedHeader))]
    public partial WordSort Sort { get; set; } = WordSort.Default;

    partial void OnSortChanged(WordSort value) => ScheduleSearch();

    public string TextHeader => Header("MOT", "MOT", WordSortField.Text);
    public string OccurrencesHeader => Header("OCCURRENCES", "OCC.", WordSortField.Occurrences);
    public string LengthHeader => Header("LONGUEUR", "LONG.", WordSortField.Length);
    public string HandsHeader => Header("MAIN(S)", "MAINS", WordSortField.Hands);
    public string LanguageHeader => Header("LANGUE", "LANG.", WordSortField.Language);
    public string ExcludedHeader => Header("EXCLU", "EXCLU", WordSortField.Excluded);

    /// <summary>
    /// Narrow table (small window): short headers, so they are not cut.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TextHeader))]
    [NotifyPropertyChangedFor(nameof(OccurrencesHeader))]
    [NotifyPropertyChangedFor(nameof(LengthHeader))]
    [NotifyPropertyChangedFor(nameof(HandsHeader))]
    [NotifyPropertyChangedFor(nameof(LanguageHeader))]
    [NotifyPropertyChangedFor(nameof(ExcludedHeader))]
    public partial bool IsCompactTable { get; set; }

    /// <summary>
    /// Usual table behavior: a click sorts by the column, a second click reverses the order.
    /// Numbers start with the biggest, the excluded words come first, the others in alphabetical order.
    /// </summary>
    [RelayCommand]
    public void SortBy(string column)
    {
        WordSortField field = Enum.Parse<WordSortField>(column);
        if (!IsSortable(field))
            return;

        Sort = Sort.Field == field
            ? Sort with { Descending = !Sort.Descending }
            : new WordSort(field, Descending: field is WordSortField.Occurrences or WordSortField.Length or WordSortField.Excluded);
    }

    /// <summary>
    /// A column filtered down to a single value (one language, one hand, only active or excluded words)
    /// has nothing to sort.
    /// </summary>
    public bool IsSortable(WordSortField field) => field switch
    {
        WordSortField.Language => LanguageSelected?.Value is null,
        WordSortField.Hands => HandsSelected?.Value is null or HandFilter.Any,
        WordSortField.Excluded => ExclusionSelected?.Value == WordExclusionFilter.All,
        _ => true,
    };

    /// <summary>
    /// Sorted column: its direction. Other sortable columns: an icon showing a click sorts them.
    /// </summary>
    private string Header(string label, string shortLabel, WordSortField field)
    {
        if (IsCompactTable)
            label = shortLabel;

        if (!IsSortable(field))
            return label;

        if (Sort.Field == field)
            return $"{label} {(Sort.Descending ? "▼" : "▲")}";

        // narrow table: no room for the sort hint
        return IsCompactTable ? label : $"{label} ↕";
    }

    private void OnColumnFilterChanged()
    {
        // the sorted column was just filtered down to a single value: back to the usual order
        if (!IsSortable(Sort.Field))
            Sort = WordSort.Default;

        OnPropertyChanged(nameof(HandsHeader));
        OnPropertyChanged(nameof(LanguageHeader));
        OnPropertyChanged(nameof(ExcludedHeader));
        ScheduleSearch();
    }

    #endregion
    #region Results

    public ObservableCollection<WordItemViewModel> Items { get; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TotalText))]
    [NotifyPropertyChangedFor(nameof(HasMore))]
    public partial int TotalCount { get; set; }

    public string TotalText
        => TotalCount switch
        {
            0 => "Aucun mot ne correspond",
            1 => "1 mot correspond",
            _ => $"{TotalCount:N0} mots correspondent",
        };

    public bool HasMore => Items.Count < TotalCount;

    [ObservableProperty]
    public partial string DatabaseText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsSearching { get; set; }

    /// <summary>
    /// A new list is being loaded (not the next page while scrolling): shown over the results
    /// only when it takes a while, so quick searches do not flash.
    /// </summary>
    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    private static readonly TimeSpan s_loadingDelay = TimeSpan.FromMilliseconds(200);
    private int _loadingVersion;

    private void BeginLoading()
    {
        int version = ++_loadingVersion;
        _ = ShowLoadingLaterAsync(version);
    }

    private async Task ShowLoadingLaterAsync(int version)
    {
        await Task.Delay(s_loadingDelay);
        if (version == _loadingVersion)
            IsLoading = true;
    }

    private void EndLoading()
    {
        _loadingVersion++;
        IsLoading = false;
    }

    [ObservableProperty]
    public partial string ErrorText { get; set; } = string.Empty;

    #endregion

    public async Task InitializeAsync()
    {
        if (!_isInitialized)
        {
            _isInitialized = true;

            Result<int> keyboardResult = _keyboardPreference.GetKeyboardType();
            KeyBoardLayoutDto? keyboard = keyboardResult.Success
                ? _keyboardLayoutAvailableService.GetKeyBoardAvailable().Find(k => (int)k.KeyBoardCode == keyboardResult.GetValue)
                : null;

            if (keyboard is not null && keyboard.KeyBoardCode.ToDomainEnum() is { Success: true } layoutResult)
                _layout = layoutResult.GetValue;
        }

        BeginLoading();
        try
        {
            await RefreshDatabaseTextAsync();
            await SearchAsync();
        }
        finally
        {
            EndLoading();
        }
    }

    private void ScheduleSearch()
    {
        if (!_isInitialized)
            return;

        _pendingSearch?.Cancel();
        _pendingSearch = new CancellationTokenSource();
        CancellationToken token = _pendingSearch.Token;

        _ = Task.Delay(s_searchDelay, token).ContinueWith(
            async _ => await SearchAsync(),
            token,
            TaskContinuationOptions.OnlyOnRanToCompletion,
            TaskScheduler.FromCurrentSynchronizationContext());
    }

    [RelayCommand]
    public async Task SearchAsync()
    {
        _currentCriteria = BuildCriteria();
        _currentSort = Sort;

        IsSearching = true;
        BeginLoading();
        ErrorText = string.Empty;
        try
        {
            WordSearchPage page = await _repository.SearchPageAsync(_currentCriteria, _currentSort, 0, PageSize);

            Items.Clear();
            foreach (var word in page.Words)
                Items.Add(new WordItemViewModel(word, _layout));

            TotalCount = page.TotalCount;
            OnPropertyChanged(nameof(HasMore));
        }
        catch (Exception ex)
        {
            ErrorText = $"Recherche impossible : {ex.Message}";
        }
        finally
        {
            IsSearching = false;
            EndLoading();
        }
    }

    [RelayCommand]
    public async Task LoadMoreAsync()
    {
        if (!HasMore || IsSearching)
            return;

        IsSearching = true;
        try
        {
            WordSearchPage page = await _repository.SearchPageAsync(_currentCriteria, _currentSort, Items.Count, PageSize);
            foreach (var word in page.Words)
                Items.Add(new WordItemViewModel(word, _layout));

            OnPropertyChanged(nameof(HasMore));
        }
        catch (Exception ex)
        {
            ErrorText = $"Chargement impossible : {ex.Message}";
        }
        finally
        {
            IsSearching = false;
        }
    }

    [RelayCommand]
    public async Task ToggleExcludedAsync(WordItemViewModel item)
    {
        bool excluded = !item.IsExcluded;
        await _repository.SetExcludedAsync(item.Id, excluded);
        item.IsExcluded = excluded;

        await RefreshDatabaseTextAsync();
    }

    private WordSearchCriteria BuildCriteria()
    {
        WordQueryBuilder builder =
            new WordQueryBuilder()
            .WithAnalyses()
            .WithExclusion(ExclusionSelected.Value)
            .WithHands(HandsSelected.Value);

        if (_layout is KeyboardLayout layout)
            builder.WithLayout(layout);

        if (LanguageSelected.Value is string language)
            builder.WithLanguages(language);

        if (!string.IsNullOrWhiteSpace(SearchText))
            builder.WithText(SearchText);

        if (!string.IsNullOrWhiteSpace(OnlyLetters))
            builder.WithOnlyLetters(OnlyLetters.Trim());

        if (int.TryParse(MinLength, out int min) && min > 0)
            builder.WithMinLength(min);

        if (int.TryParse(MaxLength, out int max) && max > 0)
            builder.WithMaxLength(max);

        if (int.TryParse(MinOccurrences, out int occurrences) && occurrences > 0)
            builder.WithMinOccurrences(occurrences);

        if (int.TryParse(MaxOccurrences, out int maxOccurrences) && maxOccurrences > 0)
            builder.WithMaxOccurrences(maxOccurrences);

        return builder.Build();
    }

    private async Task RefreshDatabaseTextAsync()
    {
        try
        {
            int active = (await _repository.SearchPageAsync(new WordSearchCriteria(), WordSort.Default, 0, 0)).TotalCount;
            int excluded = (await _repository.SearchPageAsync(
                new WordSearchCriteria { Exclusion = WordExclusionFilter.ExcludedOnly }, WordSort.Default, 0, 0)).TotalCount;

            DatabaseText = $"{active:N0} mots actifs · {excluded:N0} exclus";
        }
        catch (Exception ex)
        {
            DatabaseText = string.Empty;
            ErrorText = $"Base de mots inaccessible : {ex.Message}";
        }
    }
}
