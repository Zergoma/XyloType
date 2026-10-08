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

    /// <summary>
    /// Imports a downloaded word pack the same way: analyzed for the keyboard, written at once at the end.
    /// A word already known keeps the highest count (<see cref="OccurrenceMerge.KeepHighest"/>):
    /// a pack can be imported again, or updated, without counting its texts twice.
    /// </summary>
    Task<Result<WordImportSummary>> ImportPackAsync(
        string packFilePath,
        WordPackInfo pack,
        IKeyboardKeysLocator layout,
        IProgress<WordImportProgress>? progress = null,
        CancellationToken cancellationToken = default);
}
