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
/// A choice of a picker: the label is shown, the value is used.
/// </summary>
public record PickerOption<T>(string Label, T Value)
{
    public override string ToString() => Label;
}

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
    [ObservableProperty] public partial PickerOption<string?> LanguageSelected { get; set; }
    [ObservableProperty] public partial PickerOption<HandFilter> HandsSelected { get; set; }
    [ObservableProperty] public partial PickerOption<WordExclusionFilter> ExclusionSelected { get; set; }

    // every filter change runs the search (after a short delay while typing)
    partial void OnSearchTextChanged(string value) => ScheduleSearch();
    partial void OnOnlyLettersChanged(string value) => ScheduleSearch();
    partial void OnMinLengthChanged(string value) => ScheduleSearch();
    partial void OnMaxLengthChanged(string value) => ScheduleSearch();
    partial void OnMinOccurrencesChanged(string value) => ScheduleSearch();
    partial void OnLanguageSelectedChanged(PickerOption<string?> value) => ScheduleSearch();
    partial void OnHandsSelectedChanged(PickerOption<HandFilter> value) => ScheduleSearch();
    partial void OnExclusionSelectedChanged(PickerOption<WordExclusionFilter> value) => ScheduleSearch();

    [RelayCommand]
    public void ResetFilters()
    {
        SearchText = string.Empty;
        OnlyLetters = string.Empty;
        MinLength = string.Empty;
        MaxLength = string.Empty;
        MinOccurrences = string.Empty;
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
    public partial WordSort Sort { get; set; } = WordSort.Default;

    partial void OnSortChanged(WordSort value) => ScheduleSearch();

    public string TextHeader => Header("MOT", WordSortField.Text);
    public string OccurrencesHeader => Header("OCCURRENCES", WordSortField.Occurrences);
    public string LengthHeader => Header("LONGUEUR", WordSortField.Length);

    /// <summary>
    /// Usual table behavior: a click sorts by the column, a second click reverses the order.
    /// Numbers start with the biggest, words with "a".
    /// </summary>
    [RelayCommand]
    public void SortBy(string column)
    {
        WordSortField field = Enum.Parse<WordSortField>(column);

        Sort = Sort.Field == field
            ? Sort with { Descending = !Sort.Descending }
            : new WordSort(field, Descending: field != WordSortField.Text);
    }

    private string Header(string label, WordSortField field)
        => Sort.Field != field ? label : $"{label} {(Sort.Descending ? "▼" : "▲")}";

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

        await RefreshDatabaseTextAsync();
        await SearchAsync();
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
