using System.Collections.ObjectModel;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Microsoft.Extensions.Logging;

using XyloType.Application;
using XyloType.Application.DTOs;
using XyloType.Application.Interfaces;
using XyloType.Application.Interfaces.Typing;
using XyloType.Application.Models.Typing.Engine;
using XyloType.Application.Models.Typing.Exercices;
using XyloType.Domain.Typing.Analysis;

namespace XyloType.ViewModels.TypingLauncher;



public partial class TypingLauncherViewModel : ObservableObject
{
    public event Func<int, Task>? KeyboardLayoutChanged;

    private readonly ITypingExercicesStorage _typingExerciceStorage;
    private readonly INavigationService _navigation;
    private readonly ITypingExerciseRunService _runService;
    private readonly IUserDialogService _dialogService;
    private TypingExercices? _loadedExercises;
    private Guid? _lastSeenRunExerciseId;
    private ILogger<TypingLauncherViewModel> _logger;

    private readonly ITypingExerciseWordNumberService _typingExerciceWordNumberService;
    private readonly ITypingExerciseLineNumberService _typingExerciceLineNumberService;

    public ObservableCollection<ExerciceItemViewModel> AllExercice { get; set; } = [];

    /// <summary>
    /// The exercises by section, as shown: a title, then the tiles.
    /// </summary>
    public ObservableCollection<ExerciseGroupViewModel> Groups { get; } = [];

    private readonly IExerciseProgressService _progress;
    private readonly List<KeyBoardLayoutDto> _keyboardLayoutAvailableElem;


    public TypingLauncherViewModel(
        ITypingExercicesStorage typingExerciceStorage,
        INavigationService navigation,
        ITypingExerciseRunService runService,
        IUserDialogService dialogService,
        IKeyBoardLayoutAvailableService keyboardLayoutAvailableService,
        ILogger<TypingLauncherViewModel> logger,
        ITypingExerciseWordNumberService typingExerciceWordNumberService,
        ITypingExerciseLineNumberService typingExerciceLineNumberService,
        IExerciseProgressService progress,
        ICurrentUserService users)
    {
        _progress = progress;

        // another user: other results
        users.Changed += async (_, _) => await RefreshProgressAsync();

        _typingExerciceStorage = typingExerciceStorage;
        _navigation = navigation;
        _runService = runService;
        _dialogService = dialogService;
        _keyboardLayoutAvailableElem = keyboardLayoutAvailableService.GetKeyBoardAvailable();

        _logger = logger;
        _typingExerciceWordNumberService = typingExerciceWordNumberService;
        _typingExerciceLineNumberService = typingExerciceLineNumberService;

        Initialize();
    }

    private void Initialize()
    {
        AllExercice.CollectionChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(HasExercice));
            OnPropertyChanged(nameof(HasNoExercice));
        };

        NbLine = _typingExerciceLineNumberService.LineNumber;
        NbWordPerLine = _typingExerciceWordNumberService.ItemNumber;
    }

    private void SetKeyboardLayout(int id)
    {
        KeyBoardLayoutDto? itemKeyboard = _keyboardLayoutAvailableElem.Find(k => (int)k.KeyBoardCode == id);
        KeyboardLayoutSelected = itemKeyboard;
    }

    public IReadOnlyList<KeyBoardLayoutDto> KeyboardLayoutAvailable => _keyboardLayoutAvailableElem;

    [ObservableProperty]
    public partial KeyBoardLayoutDto? KeyboardLayoutSelected { get; set; }
    partial void OnKeyboardLayoutSelectedChanged(KeyBoardLayoutDto? value)
    {
        if (value == null)
            return;

        KeyboardLayoutChanged?.Invoke((int)value.KeyBoardCode);
    }

    [ObservableProperty]
    public partial int NbLine { get; set; }
    partial void OnNbLineChanged(int value)
    {
        Result<bool> result = _typingExerciceLineNumberService.SetLineNumber(value);
        NbLineError = result.Success ? string.Empty : result.Error;
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasNbLineError))]
    [NotifyCanExecuteChangedFor(nameof(LaunchCommand))]
    [NotifyPropertyChangedFor(nameof(IsLaunchEnabled))]
    public partial string NbLineError { get; set; } = string.Empty;

    public bool HasNbLineError => !string.IsNullOrEmpty(NbLineError);


    [ObservableProperty]
    public partial int NbWordPerLine { get; set; }
    partial void OnNbWordPerLineChanged(int value)
    {
        Result<bool> result = _typingExerciceWordNumberService.SetItemNumber(value);
        NbWordPerLineError = result.Success ? string.Empty : result.Error;
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasNbWordPerLineError))]
    [NotifyCanExecuteChangedFor(nameof(LaunchCommand))]
    [NotifyPropertyChangedFor(nameof(IsLaunchEnabled))]
    public partial string NbWordPerLineError { get; set; } = string.Empty;

    public bool HasNbWordPerLineError => !string.IsNullOrEmpty(NbWordPerLineError);

    private bool CanLaunch()
        => !IsDynamic || (!HasNbLineError && !HasNbWordPerLineError);

    /// <summary>
    /// For the custom launch button, which has no command state of its own.
    /// </summary>
    public bool IsLaunchEnabled => CanLaunch();

    public bool HasExercice => AllExercice.Count > 0;

    public bool HasNoExercice => !HasExercice;

    public async Task<Result<bool>> InitilizationAsync(int keyboardLayoutDtoId)
    {
        SetKeyboardLayout(keyboardLayoutDtoId);
        if (KeyboardLayoutSelected == null)
        {
            return Result<bool>
                .Fail($"keyboard id {keyboardLayoutDtoId} doesn't exist");
        }

        Result<TypingExercices> exercicesListLoadedResult =
            await _typingExerciceStorage.LoadAsync(KeyboardLayoutSelected.KeyBoardCode);

        if (exercicesListLoadedResult.Success)
        {
            // The list is rebuilt each time the page appears: reselect the last played
            // exercise if one was played since, otherwise keep the user selection
            Guid? playedId = _runService.CurrentExerciseId;
            Guid? toReselect = playedId is not null && playedId != _lastSeenRunExerciseId
                ? playedId
                : ExerciceSelected?.Guid;
            _lastSeenRunExerciseId = playedId;

            ExerciceSelected = null;
            AllExercice.Clear();

            _loadedExercises = exercicesListLoadedResult.GetValue;
            List<TypingExercise> exercises = _loadedExercises.Exercices;


            for (int i = 0; i < exercises.Count; i++)
            {
                AllExercice.Add(new ExerciceItemViewModel(exercises[i], i));
            }

            // the exercises are in the order of their sections: a group per section, the empty ones left out
            Groups.Clear();
            foreach (ExerciseSection section in _loadedExercises.Sections)
            {
                List<ExerciceItemViewModel> items = [.. AllExercice.Where(e => e.SectionId == section.Id)];
                if (items.Count > 0)
                    Groups.Add(new ExerciseGroupViewModel(section.Title, items));
            }

            await RefreshProgressAsync();

            ExerciceItemViewModel? previous = AllExercice.FirstOrDefault(e => e.Guid == toReselect);
            if (previous is not null)
            {
                Select(previous);
            }

            return Result<bool>
                .Ok(true);
        }
        
        return Result<bool>
            .Fail(exercicesListLoadedResult.Error);
    }


    /// <summary>
    /// Reads the results of the current user: done or not, scores.
    /// </summary>
    public async Task RefreshProgressAsync()
    {
        IReadOnlyDictionary<Guid, ScoreSummary> progress = await _progress.GetProgressAsync();

        foreach (ExerciceItemViewModel item in AllExercice)
            item.Progress = progress.GetValueOrDefault(item.Guid);

        foreach (ExerciseGroupViewModel group in Groups)
            group.RefreshProgress();
    }

    public bool IsSelectedExercice => ExerciceSelected != null;
    public bool IsNoSelection => !IsSelectedExercice;

    public int IdxSelected => ExerciceSelected?.Idx ?? -1;


    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsSelectedExercice))]
    [NotifyPropertyChangedFor(nameof(IsNoSelection))]
    [NotifyPropertyChangedFor(nameof(ExerciceName))]
    [NotifyPropertyChangedFor(nameof(ExerciceDescription))]
    [NotifyPropertyChangedFor(nameof(ExerciceLetters))]
    [NotifyPropertyChangedFor(nameof(IsDynamic))]
    [NotifyCanExecuteChangedFor(nameof(LaunchCommand))]
    [NotifyPropertyChangedFor(nameof(IsLaunchEnabled))]
    public partial ExerciceItemViewModel? ExerciceSelected { get; set; }



    public string ExerciceName => ExerciceSelected?.Name ?? "Exercices Name";
    public string ExerciceDescription => ExerciceSelected?.Desciption ?? "Exercices Description";
    public string ExerciceLetters => ExerciceSelected?.Letters ?? "Exercices Letters";

    public bool IsDynamic => ExerciceSelected?.IsDynamic ?? false;



    [RelayCommand]
    public void Select(ExerciceItemViewModel exerciceSelected)
    {
        if (ExerciceSelected == exerciceSelected)
            return;

        ExerciceSelected?.IsSelected = false;
        ExerciceSelected = exerciceSelected;
        ExerciceSelected.IsSelected = true;
    }

    [RelayCommand(CanExecute = nameof(CanLaunch))]
    public async Task Launch()
    {
        if (_loadedExercises is null
            || ExerciceSelected is null
            || KeyboardLayoutSelected is not KeyBoardLayoutDto keyboardLayoutDto)
        {
            return;
        }

        Result<IStringsProvider> stringProviderResult =
            _runService.Start(_loadedExercises, ExerciceSelected.Idx, keyboardLayoutDto);

        if (!stringProviderResult.Success)
        {
            _logger.LogWarning("Unable to start exercise: {Error}", stringProviderResult.Error);
            await _dialogService.AlertAsync("Impossible de lancer l'exercice", stringProviderResult.Error);
            return;
        }

        _logger.LogInformation(
            "Exercise started {ExerciseId} {ExerciceName}",
            ExerciceSelected.Guid,
            ExerciceSelected.Name);

        Result<bool> navigationResult = await _navigation.NavigateToTypingExerciseAsync(stringProviderResult.GetValue);
        if (!navigationResult.Success)
        {
            await _dialogService.AlertAsync("Impossible de lancer l'exercice", navigationResult.Error);
        }
    }
}
