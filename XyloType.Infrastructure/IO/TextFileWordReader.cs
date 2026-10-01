using System.Runtime.CompilerServices;

using XyloType.Application.Interfaces;
using XyloType.Domain.Text;

namespace XyloType.Infrastructure.IO;

public sealed class TextFileWordReader : IWordStreamReader
{
    // report the progress every ~1% of the file at most
    private const double ProgressStep = 0.01;

    public event Action<string>? LineChanged;

    public async IAsyncEnumerable<string> ReadWordsAsync(
        string filePath,
        string languageCode,
        IProgress<double>? progress = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        using StreamReader reader = new(filePath);

        long length = reader.BaseStream.Length;
        double lastReported = 0;

        string? line;

        while ((line = await reader.ReadLineAsync(cancellationToken)) is not null)
        {
            LineChanged?.Invoke(line);
            foreach (string word in WordTokenizer.Tokenize(line, languageCode))
            {
                yield return word;
            }

            if (progress is not null && length > 0)
            {
                // the stream is read by blocks: the position is a good enough estimate
                double fraction = Math.Min(1.0, (double)reader.BaseStream.Position / length);
                if (fraction - lastReported >= ProgressStep)
                {
                    lastReported = fraction;
                    progress.Report(fraction);
                }
            }

            await Task.Yield(); // optionnel mais propre pour gros fichiers
        }

        progress?.Report(1.0);
    }
}



public sealed class LineReader
{
    private readonly string _filePath;
    public LineReader(string filePath)
    {
        this._filePath = filePath;
    }

    public async IAsyncEnumerable<string> ReadLineAsync()
    {
        using StreamReader reader = new(_filePath);

        string? line;

        while ((line = await reader.ReadLineAsync()) is not null)
        {
            yield return line;

            await Task.Yield(); // optionnel mais propre pour gros fichiers
        }
    }
}
