using System.Linq.Expressions;

using Microsoft.EntityFrameworkCore;

using Microsoft.Extensions.Logging;
using XyloType.Domain.Entities;
using XyloType.Application.Models;
using XyloType.Application.Interfaces;
using XyloType.Infrastructure.DbContexts;
namespace XyloType.Infrastructure.Repositories;

public class DactyloRepository : IDactyloRepository
{
    private readonly IDbContextFactory<DactyloDbContext> _factory;
    private readonly ILogger<DactyloRepository> _logger;
    public DactyloRepository(
        IDbContextFactory<DactyloDbContext> factory,
        ILogger<DactyloRepository> logger)
    {
        _factory = factory;
        _logger = logger;
    }

    public async Task PersistWordsAsync(
        IReadOnlyCollection<Word> newWords,
        IReadOnlyCollection<Word> updatedWords)
    {
        await using var ctx = await _factory.CreateDbContextAsync();

        _logger.LogInformation(
            "Persist {AddedWordsCount} added words",
            newWords.Count);

        ctx.Words.AddRange(newWords);

        _logger.LogInformation(
            "Persist {UpdatedWordsCount} updated words",
            updatedWords.Count);

        ctx.Words.UpdateRange(updatedWords);

        await ctx.SaveChangesAsync();
    }

    public async Task<List<Word>> SearchAsync(WordSearchCriteria criteria)
    {
        await using var ctx = 
            await _factory.CreateDbContextAsync();

        IQueryable<Word> query = ctx.Words
            .AsQueryable();


        bool needsAnalyses =
            criteria.IncludeAnalyses ||
            criteria.FingerMask.HasValue ||
            criteria.RowMask.HasValue ||
            criteria.Layout.HasValue ||
            criteria.ExternalAccent.HasValue;

        if (needsAnalyses)
        {
            query = query.Include(w => w.Analyses);
        }

        if (criteria.LanguagesCodes?.Length  > 0 )
        {
            string[] codes = criteria.LanguagesCodes
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct()
                .ToArray();

            if (codes.Length > 0)
            {
                query = query.Where(w => codes.Contains(w.LanguageCode));
            }
        }


        if (criteria.Layout.HasValue)
        {
            query = query.Where(w =>
                w.Analyses.Any(a =>
                    (a.Layout == criteria.Layout)));
        }

        if (criteria.MinLength.HasValue)
        {
            query = query.Where(w => w.Length >= criteria.MinLength.Value);
        }

        if (criteria.MaxLength.HasValue)
        {
            query = query.Where(w => w.Length <= criteria.MaxLength.Value);
        }

        if (criteria.RowMask.HasValue)
        {
            var mask = criteria.RowMask.Value;

            query = query.Where(w =>
                w.Analyses.Any(a =>
                    (a.RowMask & mask) != 0));
        }

        if (criteria.FingerMask.HasValue)
        {
            var mask = criteria.FingerMask.Value;

            query = query.Where(w =>
                w.Analyses.Any(a =>
                    (a.FingerMask & mask) != 0));
        }


        if (criteria.ExternalAccent.HasValue)
        {
            bool extrenalAccent = criteria.ExternalAccent.Value;

            query = query.Where(w =>
                w.Analyses.Any(a =>
                    a.ExternalAccent  == extrenalAccent));
        }

        return await query.ToListAsync();
    }
}
