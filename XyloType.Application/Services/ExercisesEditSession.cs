using FluentValidation;
using FluentValidation.Results;

using XyloType.Application.DTOs;
using XyloType.Application.Interfaces;
using XyloType.Application.Interfaces.Typing;
using XyloType.Application.Models.Typing.Exercices;

namespace XyloType.Application.Services;

public class ExercisesEditSession : IExercisesEditSession
{
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

    public Result<TypingExercise> CreateNew(string name)
    {
        if (_exercises is null)
            return Result<TypingExercise>.Fail("No keyboard opened");

        TypingExercise exercise = new()
        {
            Id = _guidProvider.CreateGuid(),
            Name = name,
            TextDataType = new TypingTextDataStatic()
        };

        _exercises.Exercices.Add(exercise);
        HasChanges = true;

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
            return Result<bool>.Fail("No keyboard opened");

        int removed = _exercises.Exercices.RemoveAll(e => e.Id == id);
        if (removed == 0)
            return Result<bool>.Fail("Exercise doesn't exist");

        HasChanges = true;
        return Result<bool>.Ok(true);
    }

    public Result<bool> Move(int fromIndex, int toIndex)
    {
        if (_exercises is null)
            return Result<bool>.Fail("No keyboard opened");

        List<TypingExercise> list = _exercises.Exercices;
        if (fromIndex < 0 || fromIndex >= list.Count || toIndex < 0 || toIndex >= list.Count)
            return Result<bool>.Fail("Index out of range");

        if (fromIndex == toIndex)
            return Result<bool>.Ok(true);

        TypingExercise moved = list[fromIndex];
        list.RemoveAt(fromIndex);
        list.Insert(toIndex, moved);

        HasChanges = true;
        return Result<bool>.Ok(true);
    }

    public async Task<Result<bool>> SaveAsync()
    {
        if (_exercises is null)
            return Result<bool>.Fail("No keyboard opened");

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
