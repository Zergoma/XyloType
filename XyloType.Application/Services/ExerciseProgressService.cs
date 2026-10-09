using Microsoft.Extensions.Logging;

using XyloType.Application.Interfaces;
using XyloType.Application.Models;
using XyloType.Domain.Entities;
using XyloType.Domain.Typing.Analysis;

namespace XyloType.Application.Services;

public class ExerciseProgressService : IExerciseProgressService
{
    private readonly ICurrentUserService _users;
    private readonly IExerciseAttemptRepository _repository;
    private readonly TimeProvider _time;
    private readonly ILogger<ExerciseProgressService> _logger;

    public ExerciseProgressService(
        ICurrentUserService users,
        IExerciseAttemptRepository repository,
        TimeProvider time,
        ILogger<ExerciseProgressService> logger)
    {
        _users = users;
        _repository = repository;
        _time = time;
        _logger = logger;
    }

    public async Task<Result<ExerciseAttemptOutcome>> RecordAsync(Guid exerciseId, TypingSessionResult result)
    {
        if (_users.CurrentUser is not UserProfile user)
            return Result<ExerciseAttemptOutcome>.Fail("Aucun utilisateur : le résultat n'est pas gardé.");

        // nothing typed: nothing to keep
        if (result.Characters == 0)
            return Result<ExerciseAttemptOutcome>.Fail("Aucun caractère tapé.");

        Dictionary<Guid, List<double>> scores = await _repository.GetScoresByExerciseAsync(user.Id);
        double? previousBest = scores.TryGetValue(exerciseId, out List<double>? previous) && previous.Count > 0
            ? previous.Max()
            : null;

        ExerciseAttempt attempt = ExerciseAttempt.From(user.Id, exerciseId, result, _time.GetUtcNow().UtcDateTime);
        await _repository.AddAsync(attempt);

        _logger.LogInformation(
            "Attempt of {ExerciseId} by user {UserId}: score {Score}",
            exerciseId,
            user.Id,
            attempt.Score);

        return Result<ExerciseAttemptOutcome>.Ok(new ExerciseAttemptOutcome(attempt.Score, previousBest));
    }

    public async Task<IReadOnlyDictionary<Guid, ScoreSummary>> GetProgressAsync()
    {
        if (_users.CurrentUser is not UserProfile user)
            return new Dictionary<Guid, ScoreSummary>();

        Dictionary<Guid, List<double>> scores = await _repository.GetScoresByExerciseAsync(user.Id);

        return scores
            .Select(s => (s.Key, Summary: ScoreSummary.From(s.Value)))
            .Where(s => s.Summary is not null)
            .ToDictionary(s => s.Key, s => s.Summary!);
    }
}
