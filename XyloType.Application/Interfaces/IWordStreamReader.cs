namespace XyloType.Application.Interfaces;

public interface IWordStreamReader
{
    public event Action<string>? LineChanged;

    /// <summary>
    /// Reads the lower case words of a text file, split according to the language
    /// (elisions, compound words).
    /// </summary>
    /// <param name="progress">Receives the share of the file already read, from 0 to 1</param>
    IAsyncEnumerable<string> ReadWordsAsync(
        string filePath,
        string languageCode,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default);
}
