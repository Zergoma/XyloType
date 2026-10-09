using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using XyloType.Application.Interfaces;
using XyloType.Application.Models;
using XyloType.Domain.Entities;
using XyloType.Domain.Enums;
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

    // words written per SaveChanges: small enough to report the progress, big enough to stay fast
    private const int SaveChunkSize = 1000;

    public async Task PersistImportAsync(
        IReadOnlyCollection<Word> newWords,
        IReadOnlyCollection<Word> updatedWords,
        ImportedSource source,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default)
    {
        await using var ctx = await _factory.CreateDbContextAsync(cancellationToken);

        _logger.LogInformation(
            "Persist {AddedWordsCount} added and {UpdatedWordsCount} updated words",
            newWords.Count,
            updatedWords.Count);

        // several SaveChanges inside one transaction: all or nothing,
        // the transaction is rolled back if anything fails or is cancelled before the commit
        await using var transaction = await ctx.Database.BeginTransactionAsync(cancellationToken);

        int total = newWords.Count + updatedWords.Count;
        int done = 0;
        progress?.Report(0);

        foreach (Word[] chunk in newWords.Chunk(SaveChunkSize))
        {
            ctx.Words.AddRange(chunk);
            done += await SaveChunkAsync(ctx, chunk.Length, cancellationToken);
            progress?.Report((double)done / total);
        }

        foreach (Word[] chunk in updatedWords.Chunk(SaveChunkSize))
        {
            foreach (Word word in chunk)
            {
                // known word: only its count changes; its new analyses (no id yet) are added
                ctx.Words.Attach(word);
                ctx.Entry(word).Property(w => w.OccurrenceCount).IsModified = true;
            }

            done += await SaveChunkAsync(ctx, chunk.Length, cancellationToken);
            progress?.Report((double)done / total);
        }

        ctx.ImportedSources.Add(source);
        await ctx.SaveChangesAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);
        progress?.Report(1);
    }

    private static async Task<int> SaveChunkAsync(DactyloDbContext ctx, int count, CancellationToken cancellationToken)
    {
        await ctx.SaveChangesAsync(cancellationToken);

        // forget the saved entities: the change tracker stays small and fast
        ctx.ChangeTracker.Clear();
        return count;
    }

    public async Task<List<Word>> SearchAsync(WordSearchCriteria criteria, CancellationToken cancellationToken = default)
    {
        await using var ctx =
            await _factory.CreateDbContextAsync(cancellationToken);

        return await BuildQuery(ctx, criteria).ToListAsync(cancellationToken);
    }

    public async Task<List<WordFrequency>> SearchFrequenciesAsync(WordSearchCriteria criteria, CancellationToken cancellationToken = default)
    {
        await using var ctx =
            await _factory.CreateDbContextAsync(cancellationToken);

        // the analyses only filter (EXISTS in SQL): nothing else than two columns is read
        return await BuildQuery(ctx, criteria, loadAnalyses: false)
            .AsNoTracking()
            .Select(w => new WordFrequency(w.Text, w.OccurrenceCount))
            .ToListAsync(cancellationToken);
    }

    public async Task<WordSearchPage> SearchPageAsync(WordSearchCriteria criteria, WordSort sort, int skip, int take)
    {
        await using var ctx =
            await _factory.CreateDbContextAsync();

        IQueryable<Word> query = BuildQuery(ctx, criteria);

        int total = await query.CountAsync();

        IOrderedQueryable<Word> ordered = (sort.Field, sort.Descending) switch
        {
            (WordSortField.Text, false) => query.OrderBy(w => w.Text),
            (WordSortField.Text, true) => query.OrderByDescending(w => w.Text),
            (WordSortField.Length, false) => query.OrderBy(w => w.Length).ThenBy(w => w.Text),
            (WordSortField.Length, true) => query.OrderByDescending(w => w.Length).ThenBy(w => w.Text),
            (WordSortField.Hands, false) => OrderByHands(query, criteria.Layout, descending: false),
            (WordSortField.Hands, true) => OrderByHands(query, criteria.Layout, descending: true),
            (WordSortField.Language, false) => query.OrderBy(w => w.LanguageCode).ThenBy(w => w.Text),
            (WordSortField.Language, true) => query.OrderByDescending(w => w.LanguageCode).ThenBy(w => w.Text),
            (WordSortField.Excluded, false) => query.OrderBy(w => w.IsExcluded).ThenBy(w => w.Text),
            (WordSortField.Excluded, true) => query.OrderByDescending(w => w.IsExcluded).ThenBy(w => w.Text),
            (_, false) => query.OrderBy(w => w.OccurrenceCount).ThenBy(w => w.Text),
            _ => query.OrderByDescending(w => w.OccurrenceCount).ThenBy(w => w.Text),
        };

        List<Word> words = await ordered
            .Skip(Math.Max(0, skip))
            .Take(Math.Max(0, take))
            .ToListAsync();

        return new WordSearchPage(words, total);
    }

    public async Task SetExcludedAsync(int wordId, bool excluded)
    {
        await using var ctx =
            await _factory.CreateDbContextAsync();

        await ctx.Words
            .Where(w => w.Id == wordId)
            .ExecuteUpdateAsync(s => s.SetProperty(w => w.IsExcluded, excluded));
    }

    /// <summary>
    /// Left hand only, then right hand only, then both hands (on the given keyboard),
    /// words with a dead key after the others.
    /// </summary>
    private static IOrderedQueryable<Word> OrderByHands(IQueryable<Word> query, KeyboardLayout? layout, bool descending)
    {
        System.Linq.Expressions.Expression<Func<Word, int>> hands = w => w.Analyses
            .Where(a => layout == null || a.Layout == layout)
            .Select(a => a.UsesLeftHand && !a.UsesRightHand ? 0 : !a.UsesLeftHand && a.UsesRightHand ? 1 : 2)
            .FirstOrDefault();

        System.Linq.Expressions.Expression<Func<Word, bool>> deadKey = w => w.Analyses
            .Where(a => layout == null || a.Layout == layout)
            .Select(a => a.ExternalAccent)
            .FirstOrDefault();

        IOrderedQueryable<Word> ordered = descending
            ? query.OrderByDescending(hands).ThenByDescending(deadKey)
            : query.OrderBy(hands).ThenBy(deadKey);

        return ordered.ThenBy(w => w.Text);
    }

    private static IQueryable<Word> BuildQuery(DactyloDbContext ctx, WordSearchCriteria criteria, bool loadAnalyses = true)
    {
        IQueryable<Word> query = ctx.Words
            .AsQueryable();

        bool needsAnalyses = loadAnalyses && (
            criteria.IncludeAnalyses ||
            criteria.FingerMask.HasValue ||
            criteria.RowMask.HasValue ||
            criteria.Layout.HasValue ||
            criteria.ExternalAccent.HasValue);

        if (needsAnalyses)
        {
            query = query.Include(w => w.Analyses);
        }

        query = criteria.Exclusion switch
        {
            WordExclusionFilter.All => query,
            WordExclusionFilter.ExcludedOnly => query.Where(w => w.IsExcluded),
            _ => query.Where(w => !w.IsExcluded),
        };

        if (criteria.LanguagesCodes?.Length > 0)
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
            var layout = criteria.Layout.Value;

            query = query.Where(w =>
                w.Analyses.Any(a => a.Layout == layout));
        }

        if (!string.IsNullOrWhiteSpace(criteria.TextContains))
        {
            // words are stored lower case
            string text = criteria.TextContains.Trim().ToLowerInvariant();
            query = query.Where(w => w.Text.Contains(text));
        }

        if (!string.IsNullOrEmpty(criteria.OnlyLetters))
        {
            string set = ToGlobSet(criteria.OnlyLetters);

            // no character outside the set: NOT GLOB '*[^set]*' (an empty set matches nothing)
            query = set.Length == 0
                ? query.Where(w => false)
                : query.Where(w => !EF.Functions.Glob(w.Text, "*[^" + set + "]*"));
        }

        if (criteria.MinLength.HasValue)
        {
            query = query.Where(w => w.Length >= criteria.MinLength.Value);
        }

        if (criteria.MaxLength.HasValue)
        {
            query = query.Where(w => w.Length <= criteria.MaxLength.Value);
        }

        if (criteria.MinOccurrences.HasValue)
        {
            query = query.Where(w => w.OccurrenceCount >= criteria.MinOccurrences.Value);
        }

        if (criteria.MaxOccurrences.HasValue)
        {
            query = query.Where(w => w.OccurrenceCount <= criteria.MaxOccurrences.Value);
        }

        if (criteria.Hands != HandFilter.Any)
        {
            bool left = criteria.Hands is HandFilter.LeftOnly or HandFilter.BothHands;
            bool right = criteria.Hands is HandFilter.RightOnly or HandFilter.BothHands;
            var layout = criteria.Layout;

            query = query.Where(w =>
                w.Analyses.Any(a =>
                    (layout == null || a.Layout == layout) &&
                    a.UsesLeftHand == left &&
                    a.UsesRightHand == right));
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
                    a.ExternalAccent == extrenalAccent));
        }

        return query;
    }

    /// <summary>
    /// Characters of a GLOB set, lower case, without the ones that have a meaning in the set.
    /// </summary>
    private static string ToGlobSet(string letters)
    {
        IEnumerable<char> chars = letters
            .ToLowerInvariant()
            .Where(c => c is not ('[' or ']' or '^' or '*' or '?'))
            .Distinct();

        // a "-" is literal only at the end of the set
        string set = string.Concat(chars.Where(c => c != '-'));
        return letters.Contains('-') ? set + "-" : set;
    }
}
