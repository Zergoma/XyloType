using XyloType.Application.Models;

namespace XyloType.Application.Interfaces;

/// <summary>
/// Looks in the import history for the same text or a close title before importing a file.
/// </summary>
public interface IImportDuplicateChecker
{
    Task<ImportCheckResult> CheckAsync(string filePath, CancellationToken cancellationToken = default);
}
