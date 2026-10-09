using FluentValidation;
using FluentValidation.Results;

using XyloType.Application.DTOs;
using XyloType.Application.Interfaces;
using XyloType.Application.Interfaces.Typing;
using XyloType.Application.Models.Typing.Exercices;

namespace XyloType.Application.Services;

public class ExercisesEditSession : IExercisesEditSession
{
    private const string NoKeyboard = "No keyboard opened";
    private const string NoSection = "Section doesn't exist";

    private readonly ITypingExercicesStorage _storage;
    private readonly IValidator<TypingExercise> _exerciseValidator;
    private readonly IGuidProvider _guidProvider;

    private TypingExercices? _exercises;

    public ExercisesEditSession(
        ITypingExercicesStorage storage,
        IValidator<TypingExercise> exerciseValidator,
        IGuidProvider guidProvider)
    {
        _storage = storage;
        _exerciseValidator = exerciseValidator;
        _guidProvider = guidProvider;
    }

    public KeyBoardLayoutDto? Keyboard => _exercises?.KeyboardLayout;

    public IReadOnlyList<ExerciseSection> Sections
        => _exercises?.Sections ?? [];

    public IReadOnlyList<TypingExercise> Exercises
        => _exercises?.Exercices ?? [];

    public bool HasChanges { get; private set; }

    public async Task<Result<bool>> OpenAsync(KeyBoardLayoutDto keyboard)
    {
        Result<TypingExercices> loadResult = await _storage.LoadAsync(keyboard.KeyBoardCode);

        // no file yet for this keyboard: start with an empty list
        _exercises = loadResult.Success
            ? loadResult.GetValue
            : new TypingExercices { KeyboardLayout = keyboard };

        HasChanges = false;
        return Result<bool>.Ok(true);
    }

    public Result<TypingExercise> CreateNew(string name, Guid? sectionId)
    {
        if (_exercises is null)
            return Result<TypingExercise>.Fail(NoKeyboard);

        ExerciseSection section =
            _exercises.Sections.Find(s => s.Id == sectionId)
            ?? _exercises.Sections.LastOrDefault()
            ?? _exercises.AddSection(ExerciseSection.DefaultTitle, _guidProvider.CreateGuid());

        TypingExercise exercise = new()
        {
            Id = _guidProvider.CreateGuid(),
            Name = name,
            SectionId = section.Id,
            TextDataType = new TypingTextDataStatic()
        };

        _exercises.Exercices.Add(exercise);
        Reorder();

        return Result<TypingExercise>.Ok(exercise);
    }

    public void MarkChanged()
    {
        if (_exercises is not null)
            HasChanges = true;
    }

    public Result<bool> Remove(Guid id)
    {
        if (_exercises is null)
            return Result<bool>.Fail(NoKeyboard);

        int removed = _exercises.Exercices.RemoveAll(e => e.Id == id);
        if (removed == 0)
            return Result<bool>.Fail("Exercise doesn't exist");

        HasChanges = true;
        return Result<bool>.Ok(true);
    }

    public Result<bool> Move(Guid sectionId, int fromIndex, int toIndex)
    {
        if (_exercises is null)
            return Result<bool>.Fail(NoKeyboard);

        List<TypingExercise> inSection = [.. _exercises.ExercisesOf(sectionId)];
        if (fromIndex < 0 || fromIndex >= inSection.Count || toIndex < 0 || toIndex >= inSection.Count)
            return Result<bool>.Fail("Index out of range");

        if (fromIndex == toIndex)
            return Result<bool>.Ok(true);

        // the exercises of the section are contiguous: their place in the whole list follows the first one
        int start = _exercises.Exercices.IndexOf(inSection[0]);
        List<TypingExercise> list = _exercises.Exercices;
        TypingExercise moved = list[start + fromIndex];
        list.RemoveAt(start + fromIndex);
        list.Insert(start + toIndex, moved);

        HasChanges = true;
        return Result<bool>.Ok(true);
    }

    public Result<bool> MoveToSection(Guid exerciseId, Guid sectionId)
    {
        if (_exercises is null)
            return Result<bool>.Fail(NoKeyboard);

        if (_exercises.Exercices.Find(e => e.Id == exerciseId) is not TypingExercise exercise)
            return Result<bool>.Fail("Exercise doesn't exist");

        if (!_exercises.Sections.Exists(s => s.Id == sectionId))
            return Result<bool>.Fail(NoSection);

        if (exercise.SectionId == sectionId)
            return Result<bool>.Ok(true);

        // at the end of its new section
        _exercises.Exercices.Remove(exercise);
        _exercises.Exercices.Add(exercise);
        exercise.SectionId = sectionId;
        Reorder();

        return Result<bool>.Ok(true);
    }

    public Result<ExerciseSection> AddSection(string title)
    {
        if (_exercises is null)
            return Result<ExerciseSection>.Fail(NoKeyboard);

        ExerciseSection section = _exercises.AddSection(title.Trim(), _guidProvider.CreateGuid());
        HasChanges = true;

        return Result<ExerciseSection>.Ok(section);
    }

    public Result<bool> RenameSection(Guid sectionId, string title)
    {
        if (_exercises?.Sections.Find(s => s.Id == sectionId) is not ExerciseSection section)
            return Result<bool>.Fail(NoSection);

        if (string.IsNullOrWhiteSpace(title))
            return Result<bool>.Fail("Donnez un titre à la section.");

        section.Title = title.Trim();
        HasChanges = true;
        return Result<bool>.Ok(true);
    }

    public Result<bool> RemoveSection(Guid sectionId)
    {
        if (_exercises is null || _exercises.Sections.RemoveAll(s => s.Id == sectionId) == 0)
            return Result<bool>.Fail(NoSection);

        _exercises.Exercices.RemoveAll(e => e.SectionId == sectionId);
        HasChanges = true;
        return Result<bool>.Ok(true);
    }

    public Result<bool> MoveSection(Guid sectionId, int offset)
    {
        if (_exercises is null)
            return Result<bool>.Fail(NoKeyboard);

        List<ExerciseSection> sections = _exercises.Sections;
        int index = sections.FindIndex(s => s.Id == sectionId);
        if (index < 0)
            return Result<bool>.Fail(NoSection);

        int target = index + offset;
        if (target < 0 || target >= sections.Count)
            return Result<bool>.Fail("Out of range");

        (sections[index], sections[target]) = (sections[target], sections[index]);
        Reorder();

        return Result<bool>.Ok(true);
    }

    /// <summary>
    /// The exercises back in the order of their sections.
    /// </summary>
    private void Reorder()
    {
        _exercises!.NormalizeSections(_guidProvider.CreateGuid);
        HasChanges = true;
    }

    public async Task<Result<bool>> SaveAsync()
    {
        if (_exercises is null)
            return Result<bool>.Fail(NoKeyboard);

        List<string> errors = [];
        foreach (TypingExercise exercise in _exercises.Exercices)
        {
            ValidationResult validation = _exerciseValidator.Validate(exercise);
            if (!validation.IsValid)
            {
                string name = string.IsNullOrWhiteSpace(exercise.Name) ? "(sans nom)" : exercise.Name;
                errors.Add($"{name} : {string.Join(", ", validation.Errors.Select(e => e.ErrorMessage))}");
            }
        }

        if (errors.Count > 0)
            return Result<bool>.Fail(string.Join(Environment.NewLine, errors));

        Result<bool> saveResult = await _storage.SaveAsync(_exercises);
        if (saveResult.Success)
            HasChanges = false;

        return saveResult;
    }

    public async Task<Result<bool>> DiscardAsync()
    {
        if (_exercises is null)
            return Result<bool>.Ok(true);

        return await OpenAsync(_exercises.KeyboardLayout);
    }
}
