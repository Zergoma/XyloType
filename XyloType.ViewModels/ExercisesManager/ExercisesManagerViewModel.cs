using System.Collections.ObjectModel;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using XyloType.Application;
using XyloType.Application.DTOs;
using XyloType.Application.Interfaces;
using XyloType.Application.Interfaces.Typing;
using XyloType.Application.Mappers;
using XyloType.Application.Models;
using XyloType.Application.Models.Typing;
using XyloType.Application.Models.Typing.Exercices;
using XyloType.Application.ValueObjects;
using XyloType.Domain.Typing;
using XyloType.Domain.Enums;

namespace XyloType.ViewModels.ExercisesManager;

/// <summary>
/// Exercises menu: create, edit, delete and reorder the exercises of a keyboard.
/// Every change stays in memory until the user saves.
/// </summary>
public partial class ExercisesManagerViewModel : ObservableObject
{
    private readonly IExercisesEditSession _session;
    private readonly IPseudoWordBatchGenerator _pseudoWordBatchGenerator;
    private readonly IImportedWordsGenerator _importedWordsGenerator;
    private readonly IEditorSplitCharProvider _editorSplitCharProvider;
    private readonly IUserDialogService _dialogService;
    private readonly IUserKeyboardLayoutPreferenceService _keyboardPreference;

    private readonly List<KeyBoardLayoutDto> _keyboardLayoutAvailable;
    private readonly List<string> _languageAvailable;
    private readonly List<GeneratedTypeSourceDto> _generationTypeSourceAvailable;

    // true while the editor fields are filled from the selected exercise
    private bool _isLoadingEditor;

    // true while the keyboard picker is reverted after a cancelled switch
    private bool _isRevertingKeyboard;

    public ExercisesManagerViewModel(
        IExercisesEditSession session,
        IPseudoWordBatchGenerator pseudoWordBatchGenerator,
        IImportedWordsGenerator importedWordsGenerator,
        IEditorSplitCharProvider editorSplitCharProvider,
        IUserDialogService dialogService,
        IUserKeyboardLayoutPreferenceService keyboardPreference,
        IKeyBoardLayoutAvailableService keyboardLayoutAvailableService,
        ILanguageAvailableService languageAvailableService,
        IGenerationTypeSourceAvailableService generationTypeSourceAvailableService)
    {
        _session = session;
        _pseudoWordBatchGenerator = pseudoWordBatchGenerator;
        _importedWordsGenerator = importedWordsGenerator;
        _editorSplitCharProvider = editorSplitCharProvider;
        _dialogService = dialogService;
        _keyboardPreference = keyboardPreference;

        _keyboardLayoutAvailable = keyboardLayoutAvailableService.GetKeyBoardAvailable();
        _languageAvailable = languageAvailableService.GetAvailableLanguage();
        _generationTypeSourceAvailable = generationTypeSourceAvailableService.GetGenerationTypeSourceAvailable();
    }

    #region Keyboard and list

    public IReadOnlyList<KeyBoardLayoutDto> KeyboardLayoutAvailable => _keyboardLayoutAvailable;

    [ObservableProperty]
    public partial KeyBoardLayoutDto? KeyboardLayoutSelected { get; set; }

    partial void OnKeyboardLayoutSelectedChanged(KeyBoardLayoutDto? oldValue, KeyBoardLayoutDto? newValue)
    {
        if (_isRevertingKeyboard || newValue is null || ReferenceEquals(oldValue, newValue))
            return;

        _ = SwitchKeyboardAsync(oldValue, newValue);
    }

    /// <summary>
    /// The exercises, by section.
    /// </summary>
    public ObservableCollection<ExerciseSectionViewModel> Sections { get; } = [];

    private IEnumerable<ExerciseListItemViewModel> AllItems => Sections.SelectMany(s => s.Items);

    public bool HasItems => Sections.Count > 0;
    public bool HasNoItems => !HasItems;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelection))]
    [NotifyPropertyChangedFor(nameof(HasNoSelection))]
    [NotifyCanExecuteChangedFor(nameof(DeleteExerciseCommand))]
    public partial ExerciseListItemViewModel? SelectedItem { get; set; }

    public bool HasSelection => SelectedItem is not null;
    public bool HasNoSelection => !HasSelection;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    [NotifyCanExecuteChangedFor(nameof(CancelCommand))]
    public partial bool HasChanges { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasStatus))]
    public partial string StatusMessage { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsStatusError { get; set; }

    public bool HasStatus => !string.IsNullOrEmpty(StatusMessage);

    /// <summary>
    /// Opens the exercises of the keyboard saved in the user preferences.
    /// </summary>
    public async Task InitializeAsync()
    {
        if (KeyboardLayoutSelected is not null)
            return;

        Result<int> keyboardResult = _keyboardPreference.GetKeyboardType();
        KeyBoardLayoutDto? keyboard =
            keyboardResult.Success
                ? _keyboardLayoutAvailable.Find(k => (int)k.KeyBoardCode == keyboardResult.GetValue)
                : null;

        keyboard ??= _keyboardLayoutAvailable.FirstOrDefault();
        if (keyboard is null)
            return;

        _isRevertingKeyboard = true;
        KeyboardLayoutSelected = keyboard;
        _isRevertingKeyboard = false;

        await OpenKeyboardAsync(keyboard);
    }

    /// <summary>
    /// Reads the saved exercises again (after a pack was imported), when nothing is being edited.
    /// </summary>
    public async Task ReloadAsync()
    {
        if (KeyboardLayoutSelected is null || _session.HasChanges)
            return;

        Guid? selectedId = SelectedItem?.Id;
        await _session.OpenAsync(KeyboardLayoutSelected);
        RebuildItems(selectedId);
    }

    private async Task SwitchKeyboardAsync(KeyBoardLayoutDto? previous, KeyBoardLayoutDto next)
    {
        if (!await ConfirmDiscardChangesAsync())
        {
            _isRevertingKeyboard = true;
            KeyboardLayoutSelected = previous;
            _isRevertingKeyboard = false;
            return;
        }

        await OpenKeyboardAsync(next);
    }

    private async Task OpenKeyboardAsync(KeyBoardLayoutDto keyboard)
    {
        await _session.OpenAsync(keyboard);
        RebuildItems(selectId: null);
        SetStatus(string.Empty);
    }

    /// <summary>
    /// Builds the sections and their exercises again from the session.
    /// </summary>
    private void RebuildItems(Guid? selectId)
    {
        SelectItem(null);
        Sections.Clear();

        foreach (ExerciseSection section in _session.Sections)
        {
            ExerciseSectionViewModel sectionVm = new(section);
            sectionVm.LevelChanged += (_, _) =>
            {
                _session.MarkChanged();
                HasChanges = _session.HasChanges;
            };
            foreach (TypingExercise exercise in _session.Exercises.Where(e => e.SectionId == section.Id))
                sectionVm.Items.Add(new ExerciseListItemViewModel(exercise));

            Sections.Add(sectionVm);
        }

        UpdateSectionPlaces();
        OnPropertyChanged(nameof(HasItems));
        OnPropertyChanged(nameof(HasNoItems));
        HasChanges = _session.HasChanges;

        ExerciseListItemViewModel? toSelect = AllItems.FirstOrDefault(i => i.Id == selectId);
        if (toSelect is not null)
            SelectItem(toSelect);
    }

    private void UpdateSectionPlaces()
    {
        for (int i = 0; i < Sections.Count; i++)
        {
            Sections[i].IsFirst = i == 0;
            Sections[i].IsLast = i == Sections.Count - 1;
            Sections[i].Refresh();
        }
    }

    private ExerciseSectionViewModel? SectionOf(ExerciseListItemViewModel? item)
        => item is null ? null : Sections.FirstOrDefault(s => s.Items.Contains(item));

    [RelayCommand]
    public void Select(ExerciseListItemViewModel item)
        => SelectItem(item);

    private void SelectItem(ExerciseListItemViewModel? item)
    {
        if (SelectedItem == item)
            return;

        SelectedItem?.IsSelected = false;
        SelectedItem = item;
        SelectedItem?.IsSelected = true;

        LoadEditor(item?.Exercise);
    }

    /// <summary>
    /// Moves an exercise inside its section (drag and drop), indexes in the section.
    /// </summary>
    public void Move(ExerciseSectionViewModel section, int fromIndex, int toIndex)
    {
        if (fromIndex == toIndex || !_session.Move(section.Id, fromIndex, toIndex).Success)
            return;

        section.Items.Move(fromIndex, toIndex);
        HasChanges = _session.HasChanges;
    }

    [RelayCommand]
    public void NewExercise()
    {
        // in the section of the exercise being edited, else the last one
        Guid? sectionId = SectionOf(SelectedItem)?.Id;

        Result<TypingExercise> createResult = _session.CreateNew("Nouvel exercice", sectionId);
        if (!createResult.Success)
        {
            SetStatus(createResult.Error, isError: true);
            return;
        }

        RebuildItems(createResult.GetValue.Id);
    }

    [RelayCommand(CanExecute = nameof(HasSelection))]
    public async Task DeleteExercise()
    {
        if (SelectedItem is not ExerciseListItemViewModel item || SectionOf(item) is not ExerciseSectionViewModel section)
            return;

        bool confirmed = await _dialogService.ConfirmAsync(
            "Supprimer l'exercice",
            $"Supprimer « {item.Name} » ? La suppression sera effective à l'enregistrement.",
            "Supprimer",
            "Annuler");

        if (!confirmed || !_session.Remove(item.Id).Success)
            return;

        int index = section.Items.IndexOf(item);
        SelectItem(null);
        section.Items.Remove(item);
        section.Refresh();
        HasChanges = _session.HasChanges;

        // select the neighbour to keep editing smoothly
        if (section.Items.Count > 0)
            SelectItem(section.Items[Math.Min(index, section.Items.Count - 1)]);
    }

    #region Sections

    [RelayCommand]
    public async Task NewSection()
    {
        string? title = await _dialogService.PromptAsync(
            "Nouvelle section",
            "Titre de la section :",
            "Créer",
            "Annuler",
            string.Empty,
            SectionTitleMaxLength);

        if (string.IsNullOrWhiteSpace(title))
            return;

        Result<ExerciseSection> result = _session.AddSection(title);
        if (!result.Success)
        {
            SetStatus(result.Error, isError: true);
            return;
        }

        RebuildItems(SelectedItem?.Id);
    }

    [RelayCommand]
    public async Task RenameSection(ExerciseSectionViewModel section)
    {
        string? title = await _dialogService.PromptAsync(
            "Renommer la section",
            "Titre de la section :",
            "Renommer",
            "Annuler",
            section.Title,
            SectionTitleMaxLength);

        if (title is null || title.Trim() == section.Title)
            return;

        Result<bool> result = _session.RenameSection(section.Id, title);
        if (!result.Success)
        {
            SetStatus(result.Error, isError: true);
            return;
        }

        section.Refresh();
        HasChanges = _session.HasChanges;
    }

    [RelayCommand]
    public async Task DeleteSection(ExerciseSectionViewModel section)
    {
        string exercises = section.Items.Count switch
        {
            0 => string.Empty,
            1 => " et son exercice",
            int n => $" et ses {n} exercices",
        };

        bool confirmed = await _dialogService.ConfirmAsync(
            "Supprimer la section",
            $"Supprimer « {section.Title} »{exercises} ? La suppression sera effective à l'enregistrement.",
            "Supprimer",
            "Annuler");

        if (!confirmed || !_session.RemoveSection(section.Id).Success)
            return;

        Guid? selectedId = SectionOf(SelectedItem) == section ? null : SelectedItem?.Id;
        RebuildItems(selectedId);
    }

    [RelayCommand]
    public void MoveSectionUp(ExerciseSectionViewModel section) => MoveSection(section, -1);

    [RelayCommand]
    public void MoveSectionDown(ExerciseSectionViewModel section) => MoveSection(section, +1);

    private void MoveSection(ExerciseSectionViewModel section, int offset)
    {
        if (!_session.MoveSection(section.Id, offset).Success)
            return;

        int index = Sections.IndexOf(section);
        Sections.Move(index, index + offset);
        UpdateSectionPlaces();
        HasChanges = _session.HasChanges;
    }

    private const int SectionTitleMaxLength = 60;

    #endregion

    [RelayCommand(CanExecute = nameof(HasChanges))]
    public async Task Save()
    {
        Result<bool> saveResult = await _session.SaveAsync();
        if (!saveResult.Success)
        {
            SetStatus(saveResult.Error, isError: true);
            return;
        }

        HasChanges = false;
        SetStatus("Exercices enregistrés");
    }

    [RelayCommand(CanExecute = nameof(HasChanges))]
    public async Task Cancel()
    {
        bool confirmed = await _dialogService.ConfirmAsync(
            "Annuler les modifications",
            "Toutes les modifications non enregistrées seront perdues.",
            "Annuler les modifications",
            "Continuer l'édition");

        if (!confirmed)
            return;

        Guid? selectedId = SelectedItem?.Id;
        await _session.DiscardAsync();
        RebuildItems(selectedId);
        SetStatus("Modifications annulées");
    }

    /// <summary>
    /// Asks the user before losing unsaved changes (keyboard switch, leaving the page).
    /// </summary>
    /// <returns>true if there is nothing to lose or the user accepted to lose it</returns>
    public async Task<bool> ConfirmDiscardChangesAsync()
    {
        if (!_session.HasChanges)
            return true;

        bool confirmed = await _dialogService.ConfirmAsync(
            "Modifications non enregistrées",
            "Les modifications seront perdues. Continuer ?",
            "Perdre les modifications",
            "Rester");

        if (confirmed)
        {
            await _session.DiscardAsync();
            RebuildItems(SelectedItem?.Id);
        }

        return confirmed;
    }

    private void SetStatus(string message, bool isError = false)
    {
        IsStatusError = isError;
        StatusMessage = message;
    }

    #endregion

    #region Editor

    public IReadOnlyList<string> LanguageAvailable => _languageAvailable;
    public IReadOnlyList<GeneratedTypeSourceDto> GenerationTypeSourceAvailable => _generationTypeSourceAvailable;

    [ObservableProperty]
    public partial string ExerciseName { get; set; } = string.Empty;

    /// <summary>
    /// The section of the exercise being edited: another one moves it to the end of that section.
    /// </summary>
    [ObservableProperty]
    public partial ExerciseSectionViewModel? EditorSection { get; set; }

    partial void OnEditorSectionChanged(ExerciseSectionViewModel? value)
    {
        if (_isLoadingEditor || value is null || SelectedItem is not ExerciseListItemViewModel item || SectionOf(item) == value)
            return;

        if (!_session.MoveToSection(item.Id, value.Id).Success)
            return;

        RebuildItems(item.Id);
    }

    [ObservableProperty]
    public partial string Description { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string AllowedChars { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsDynamic))]
    [NotifyPropertyChangedFor(nameof(IsRealWords))]
    [NotifyPropertyChangedFor(nameof(BadgeText))]
    [NotifyPropertyChangedFor(nameof(StaticDynamicText))]
    public partial bool IsStatic { get; set; } = true;

    public bool IsDynamic
    {
        get => !IsStatic;
        set => IsStatic = !value;
    }

    public string StaticDynamicText
        => IsStatic ? "Texte fixe" : "Texte généré à chaque partie";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TextLengthText))]
    [NotifyPropertyChangedFor(nameof(IsTextTooLong))]
    public partial string GeneratedText { get; set; } = string.Empty;

    /// <summary>
    /// Characters of the fixed text out of the limit, e.g. "1 234 / 4 000".
    /// </summary>
    public string TextLengthText => $"{GeneratedText.Length:N0} / {ExerciseSizeLimits.MaxTextLength:N0}";

    /// <summary>
    /// A text saved before the limit may be longer: it is shown, the user shortens it (saving is refused until then).
    /// </summary>
    public bool IsTextTooLong => GeneratedText.Length > ExerciseSizeLimits.MaxTextLength;

    /// <summary>
    /// Limit of the text field: the limit, or the length of a longer text loaded (the field would cut it).
    /// </summary>
    [ObservableProperty]
    public partial int TextMaxLength { get; set; } = ExerciseSizeLimits.MaxTextLength;

    [ObservableProperty]
    public partial int LineCount { get; set; } = 3;

    [ObservableProperty]
    public partial int WordsPerLine { get; set; } = 8;

    // a value out of the limits is brought back at once: the field shows what will be generated
    partial void OnLineCountChanged(int value)
    {
        int clamped = ExerciseSizeLimits.ClampLines(value);
        if (clamped != value)
            LineCount = clamped;
    }

    partial void OnWordsPerLineChanged(int value)
    {
        int clamped = ExerciseSizeLimits.ClampWordsPerLine(value);
        if (clamped != value)
            WordsPerLine = clamped;
    }

    [ObservableProperty]
    public partial int MinLengthWord { get; set; } = 3;

    [ObservableProperty]
    public partial int MaxLengthWord { get; set; } = 5;

    [ObservableProperty]
    public partial string? LanguageSelected { get; set; }

    [ObservableProperty]
    public partial GeneratedTypeSourceDto? GenerationTypeSourceSelected { get; set; }

    /// <summary>
    /// Only the imported words come in a language: invented pseudo words do not need one.
    /// </summary>
    public bool IsLanguageUsed => GenerationTypeSourceSelected == GeneratedTypeSourceDto.Words;

    /// <summary>
    /// Badge of a generated exercise, as on the home page: words drawn again at each game,
    /// real ones (imported) or invented.
    /// </summary>
    public bool IsRealWords => IsDynamic && IsLanguageUsed;

    public string BadgeText => IsRealWords ? "Vrais mots" : "Mots inventés";

    partial void OnExerciseNameChanged(string value) => ApplyEditor();
    partial void OnDescriptionChanged(string value) => ApplyEditor();
    partial void OnAllowedCharsChanged(string value) => ApplyEditor();
    partial void OnIsStaticChanged(bool value) => ApplyEditor();
    partial void OnMinLengthWordChanged(int value) => ApplyEditor();
    partial void OnMaxLengthWordChanged(int value) => ApplyEditor();
    partial void OnLanguageSelectedChanged(string? value) => ApplyEditor();
    partial void OnGenerationTypeSourceSelectedChanged(GeneratedTypeSourceDto? value)
    {
        OnPropertyChanged(nameof(IsLanguageUsed));
        OnPropertyChanged(nameof(IsRealWords));
        OnPropertyChanged(nameof(BadgeText));
        ApplyEditor();
    }

    partial void OnGeneratedTextChanged(string value)
    {
        // an exercise loaded: its text is never cut, even when longer than the limit
        if (_isLoadingEditor)
            TextMaxLength = Math.Max(ExerciseSizeLimits.MaxTextLength, value.Length);

        // letters typed in the text are added to the allowed letters
        if (!_isLoadingEditor)
        {
            string missing = string.Concat(value
                .Where(c => !char.IsWhiteSpace(c) && !AllowedChars.Contains(c))
                .Distinct());

            if (missing.Length > 0)
            {
                AllowedChars += missing;
                return; // AllowedChars change already applied the editor
            }
        }

        ApplyEditor();
    }

    [RelayCommand]
    public void SwitchStaticDynamic()
        => IsStatic = !IsStatic;

    [RelayCommand]
    public async Task GenerateWords()
    {
        int lineCount = ExerciseSizeLimits.ClampLines(LineCount);
        int wordsPerLine = ExerciseSizeLimits.ClampWordsPerLine(WordsPerLine);
        int count = lineCount * wordsPerLine;
        int minLength = Math.Min(MinLengthWord, MaxLengthWord);
        int maxLength = Math.Max(MinLengthWord, MaxLengthWord);

        Result<List<string>> result =
            GenerationTypeSourceSelected == GeneratedTypeSourceDto.Words
                ? await GenerateImportedWordsAsync(count, minLength, maxLength)
                : _pseudoWordBatchGenerator.Generate(count, new PseudoWordOptions(AllowedChars, minLength, maxLength));

        if (!result.Success)
        {
            SetStatus(result.Error, isError: true);
            return;
        }

        // real line breaks (the editor ones), not ↵: a ↵ is a key to type and stays up to the user;
        // very long words may go beyond the limit: the last lines are left out, no word is cut
        string separator = _editorSplitCharProvider.GetSplitCharacter().ToString();
        GeneratedText = string.Join(
            separator,
            ExerciseSizeLimits.FitLines(result.GetValue.Chunk(wordsPerLine).Select(line => string.Join(' ', line)), separator));
        SetStatus(string.Empty);
    }

    /// <summary>
    /// Real words of the selected language, among the imported ones, typable with the allowed letters.
    /// </summary>
    private async Task<Result<List<string>>> GenerateImportedWordsAsync(int count, int minLength, int maxLength)
    {
        if (KeyboardLayoutSelected is null)
            return Result<List<string>>.Fail("Aucun clavier sélectionné");

        Result<KeyboardLayout> layoutResult = KeyboardLayoutSelected.KeyBoardCode.ToDomainEnum();
        if (!layoutResult.Success)
            return Result<List<string>>.Fail(layoutResult.Error);

        return await _importedWordsGenerator.GenerateAsync(
            new ImportedWordsOptions(
                LanguageSelected is null ? [] : [LanguageSelected],
                AllowedChars,
                minLength,
                maxLength,
                layoutResult.GetValue),
            count);
    }

    private void LoadEditor(TypingExercise? exercise)
    {
        _isLoadingEditor = true;
        try
        {
            ExerciseName = exercise?.Name ?? string.Empty;
            EditorSection = Sections.FirstOrDefault(s => s.Id == exercise?.SectionId);
            Description = exercise?.Description ?? string.Empty;
            AllowedChars = exercise?.AllowedCharacters ?? string.Empty;

            switch (exercise?.TextDataType)
            {
                case TypingTextDataDynamic dynamic:
                    IsStatic = false;
                    GeneratedText = string.Empty;
                    MinLengthWord = dynamic.LengthMin;
                    MaxLengthWord = dynamic.LengthMax;
                    LanguageSelected = dynamic.LanguagesSelected.FirstOrDefault();
                    Result<GeneratedTypeSourceDto> sourceResult = dynamic.GeneratedTypeSource.ToDto();
                    GenerationTypeSourceSelected = sourceResult.Success ? sourceResult.GetValue : GeneratedTypeSourceDto.PseudoWords;
                    break;

                case TypingTextDataStatic staticData:
                    IsStatic = true;
                    GeneratedText = staticData.GeneratedText;
                    LanguageSelected = null;
                    GenerationTypeSourceSelected = GeneratedTypeSourceDto.PseudoWords;
                    break;

                default:
                    IsStatic = true;
                    GeneratedText = string.Empty;
                    break;
            }
        }
        finally
        {
            _isLoadingEditor = false;
        }
    }

    /// <summary>
    /// Writes the editor fields into the selected exercise (in memory only).
    /// </summary>
    private void ApplyEditor()
    {
        if (_isLoadingEditor || SelectedItem is not ExerciseListItemViewModel item)
            return;

        TypingExercise exercise = item.Exercise;
        exercise.Name = ExerciseName.Trim();
        exercise.Description = Description;

        if (IsStatic)
        {
            exercise.TextDataType = new TypingTextDataStatic { GeneratedText = GeneratedText };

            // keep the order of the allowed letters, restricted to the ones used in the text
            exercise.AllowedCharacters =
                string.IsNullOrWhiteSpace(GeneratedText)
                    ? AllowedChars
                    : AllowedLettersExtractor.ExtractAllowedLetters(AllowedChars, GeneratedText);
        }
        else
        {
            Result<GeneratedTypeSource> sourceResult =
                (GenerationTypeSourceSelected ?? GeneratedTypeSourceDto.PseudoWords).ToModel();

            exercise.TextDataType = new TypingTextDataDynamic
            {
                LengthMin = Math.Min(MinLengthWord, MaxLengthWord),
                LengthMax = Math.Max(MinLengthWord, MaxLengthWord),
                LanguagesSelected = LanguageSelected is null ? [] : [LanguageSelected],
                GeneratedTypeSource = sourceResult.Success ? sourceResult.GetValue : GeneratedTypeSource.PseudoWords
            };
            exercise.AllowedCharacters = AllowedChars;
        }

        _session.MarkChanged();
        HasChanges = _session.HasChanges;
        item.Refresh();
    }

    #endregion
}
