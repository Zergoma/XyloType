using XyloType.Application.Models;
using XyloType.Domain.Entities;

namespace XyloType.Application.Interfaces;

public interface IDactyloRepository
{
    /// <summary>
    /// Writes the result of an import in one transaction: the new words, the updated ones
    /// and the history entry, or nothing at all.
    /// </summary>
    /// <param name="progress">Receives the share of the words written, from 0 to 1</param>
    Task PersistImportAsync(
        IReadOnlyCollection<Word> newWords,
        IReadOnlyCollection<Word> updatedWords,
        ImportedSource source,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default);

    Task<List<Word>> SearchAsync(WordSearchCriteria criteria, CancellationToken cancellationToken = default);

    /// <summary>
    /// One sorted page of the words matching the criteria, with the total count.
    /// </summary>
    Task<WordSearchPage> SearchPageAsync(WordSearchCriteria criteria, WordSort sort, int skip, int take);

    Task SetExcludedAsync(int wordId, bool excluded);
}
