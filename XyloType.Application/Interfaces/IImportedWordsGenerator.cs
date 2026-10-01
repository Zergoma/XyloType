using XyloType.Application.Models;

namespace XyloType.Application.Interfaces;

/// <summary>
/// Picks real words among the imported ones.
/// </summary>
public interface IImportedWordsGenerator
{
    /// <summary>
    /// Picks <paramref name="count"/> words matching the options; frequent words come up more often.
    /// </summary>
    /// <returns>Fail if no imported word matches</returns>
    Task<Result<List<string>>> GenerateAsync(ImportedWordsOptions options, int count);
}
