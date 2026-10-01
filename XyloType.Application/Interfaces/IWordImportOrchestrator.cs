using XyloType.Application.Models;

namespace XyloType.Application.Interfaces;

public interface IWordImportOrchestrator
{
    /// <summary>
    /// Reads the words of a text file, analyzes them for the keyboard and stores them.
    /// Nothing is written before the whole file has been read: a cancelled or failed import changes nothing.
    /// </summary>
    /// <param name="progress">Receives the words read and the share of the file read</param>
    Task<Result<WordImportSummary>> ImportAsync(
        string filePath,
        string languageCode,
        IKeyboardKeysLocator layout,
        IProgress<WordImportProgress>? progress = null,
        CancellationToken cancellationToken = default);
}
