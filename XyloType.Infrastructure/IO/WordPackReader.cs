using System.Globalization;
using System.IO.Compression;
using System.Runtime.CompilerServices;

using XyloType.Application.Interfaces;
using XyloType.Application.Models;

namespace XyloType.Infrastructure.IO;

/// <summary>
/// Reads a word pack (see <see cref="WordPackFormat"/>): gzip, one "word TAB occurrences" per line.
/// Malformed lines are skipped rather than failing the whole import.
/// </summary>
public sealed class WordPackReader : IWordPackReader
{
    // report the progress every ~1% of the file at most
    private const double ProgressStep = 0.01;

    public async IAsyncEnumerable<(string Text, int Occurrences)> ReadAsync(
        string filePath,
        IProgress<double>? progress = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await using FileStream file = File.OpenRead(filePath);
        await using GZipStream unzipped = new(file, CompressionMode.Decompress);
        using StreamReader reader = new(unzipped);

        // the position in the compressed file gives the progress
        long length = file.Length;
        double lastReported = 0;

        string? line;
        while ((line = await reader.ReadLineAsync(cancellationToken)) is not null)
        {
            if (TryParse(line, out string text, out int occurrences))
                yield return (text, occurrences);

            if (progress is not null && length > 0)
            {
                double fraction = Math.Min(1.0, (double)file.Position / length);
                if (fraction - lastReported >= ProgressStep)
                {
                    lastReported = fraction;
                    progress.Report(fraction);
                }
            }
        }

        progress?.Report(1.0);
    }

    internal static bool TryParse(string line, out string text, out int occurrences)
    {
        text = string.Empty;
        occurrences = 0;

        if (line.Length == 0 || line[0] == WordPackFormat.Comment)
            return false;

        int separator = line.IndexOf(WordPackFormat.Separator);
        if (separator <= 0)
            return false;

        text = line[..separator].Trim();
        return text.Length > 0
            && !text.Any(char.IsWhiteSpace)
            && int.TryParse(line.AsSpan(separator + 1), NumberStyles.None, CultureInfo.InvariantCulture, out occurrences)
            && occurrences > 0;
    }
}
