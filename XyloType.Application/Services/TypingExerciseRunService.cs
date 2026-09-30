using XyloType.Application.DTOs;
using XyloType.Application.Interfaces;
using XyloType.Application.Models.Typing.Engine;
using XyloType.Application.Models.Typing.Exercices;

namespace XyloType.Application.Services;

public class TypingExerciseRunService : ITypingExerciseRunService
{
    private readonly ICreateStringProviderOrchestrator _createStringProviderOrchestrator;

    private TypingExercices? _exercises;
    private TypingExercicesEngine? _engine;
    private KeyBoardLayoutDto? _keyboard;
    private int _idx = -1;

    public TypingExerciseRunService(ICreateStringProviderOrchestrator createStringProviderOrchestrator)
    {
        _createStringProviderOrchestrator = createStringProviderOrchestrator;
    }

    public Guid? CurrentExerciseId
        => CurrentExercise?.Id;

    public string CurrentExerciseName
        => CurrentExercise?.Name ?? string.Empty;

    public bool HasNext
        => _exercises is not null && _idx + 1 < _exercises.Exercices.Count;

    private TypingExercise? CurrentExercise
        => _exercises is not null && _idx >= 0 && _idx < _exercises.Exercices.Count
            ? _exercises.Exercices[_idx]
            : null;

    public Result<IStringsProvider> Start(TypingExercices exercises, int idx, KeyBoardLayoutDto keyboard)
    {
        if (idx < 0 || idx >= exercises.Exercices.Count)
        {
            return Result<IStringsProvider>
                .Fail($"No exercise at index {idx}");
        }

        _exercises = exercises;
        _engine = new TypingExercicesEngine(exercises, idx);
        _keyboard = keyboard;
        _idx = idx;

        return CreateCurrent();
    }

    public Result<IStringsProvider> Retry()
        => CreateCurrent();

    public Result<IStringsProvider> Next()
    {
        if (_engine is null)
        {
            return Result<IStringsProvider>
                .Fail("No exercise started");
        }

        Result<TypingExercise> nextResult = _engine.NextExercice();
        if (!nextResult.Success)
        {
            return Result<IStringsProvider>
                .Fail(nextResult.Error);
        }

        _idx++;
        return CreateCurrent();
    }

    private Result<IStringsProvider> CreateCurrent()
    {
        if (_engine is null || _keyboard is null)
        {
            return Result<IStringsProvider>
                .Fail("No exercise started");
        }

        Result<TypingExercise> currentResult = _engine.CurrentExercice();
        if (!currentResult.Success)
        {
            return Result<IStringsProvider>
                .Fail(currentResult.Error);
        }

        Result<IStringsProvider> providerResult =
            _createStringProviderOrchestrator.Create(currentResult.GetValue, _keyboard);

        return providerResult;
    }
}
