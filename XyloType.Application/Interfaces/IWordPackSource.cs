using XyloType.Application.Models;

namespace XyloType.Application.Interfaces;

/// <summary>
/// Where the word packs are published (a release of the GitHub repository).
/// </summary>
public interface IWordPackSource
{
    /// <summary>
    /// The packs available.
    /// </summary>
    Task<Result<WordPackCatalog>> GetCatalogAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Downloads a pack to a local file, checked against its checksum.
    /// </summary>
    /// <param name="progress">Receives the share of the file downloaded, from 0 to 1</param>
    /// <returns>The path of the downloaded file</returns>
    Task<Result<string>> DownloadAsync(
        WordPackInfo pack,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Reads a pack file (see <see cref="WordPackFormat"/>).
/// </summary>
public interface IWordPackReader
{
    /// <param name="progress">Receives the share of the file read, from 0 to 1</param>
    IAsyncEnumerable<(string Text, int Occurrences)> ReadAsync(
        string filePath,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default);
}
