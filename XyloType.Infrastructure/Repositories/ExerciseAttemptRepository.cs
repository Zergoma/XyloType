using Microsoft.EntityFrameworkCore;

using XyloType.Application.Interfaces;
using XyloType.Domain.Entities;
using XyloType.Infrastructure.DbContexts;

namespace XyloType.Infrastructure.Repositories;

public class ExerciseAttemptRepository : IExerciseAttemptRepository
{
    private readonly IDbContextFactory<DactyloDbContext> _factory;

    public ExerciseAttemptRepository(IDbContextFactory<DactyloDbContext> factory)
    {
        _factory = factory;
    }

    public async Task AddAsync(ExerciseAttempt attempt)
    {
        await using var ctx = await _factory.CreateDbContextAsync();

        ctx.ExerciseAttempts.Add(attempt);
        await ctx.SaveChangesAsync();
    }

    public async Task<Dictionary<Guid, List<double>>> GetScoresByExerciseAsync(int userId)
    {
        await using var ctx = await _factory.CreateDbContextAsync();

        var scores = await ctx.ExerciseAttempts
            .AsNoTracking()
            .Where(a => a.UserId == userId)
            .Select(a => new { a.ExerciseId, a.Score })
            .ToListAsync();

        return scores
            .GroupBy(s => s.ExerciseId)
            .ToDictionary(g => g.Key, g => g.Select(s => s.Score).ToList());
    }

    public async Task<Dictionary<int, int>> CountByUserAsync()
    {
        await using var ctx = await _factory.CreateDbContextAsync();

        return await ctx.ExerciseAttempts
            .GroupBy(a => a.UserId)
            .Select(g => new { UserId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(g => g.UserId, g => g.Count);
    }
}
