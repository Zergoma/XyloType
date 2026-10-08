using System.Globalization;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;

using XyloType.Application.Models;

namespace XyloType.Infrastructure.IO;

/// <summary>
/// Writes a word pack (see <see cref="WordPackFormat"/>), read back by <see cref="WordPackReader"/>.
/// </summary>
public static class WordPackWriter
{
    /// <param name="comments">Lines put at the top of the file, after '#'</param>
    /// <returns>The checksum (SHA-256, lower case hex), the size of the file and the number of words written</returns>
    public static async Task<(string Sha256, long Size, int WordCount)> WriteAsync(
        string filePath,
        IEnumerable<(string Text, int Occurrences)> words,
        IEnumerable<string> comments,
        CancellationToken cancellationToken = default)
    {
        int count = 0;

        await using (FileStream file = File.Create(filePath))
        await using (GZipStream zipped = new(file, CompressionLevel.SmallestSize))
        await using (StreamWriter writer = new(zipped, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false)) { NewLine = "\n" })
        {
            foreach (string comment in comments)
                await writer.WriteLineAsync($"{WordPackFormat.Comment} {comment}");

            foreach ((string text, int occurrences) in words)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await writer.WriteLineAsync(
                    $"{text}{WordPackFormat.Separator}{occurrences.ToString(CultureInfo.InvariantCulture)}");
                count++;
            }
        }

        await using FileStream written = File.OpenRead(filePath);
        string sha256 = Convert.ToHexStringLower(await SHA256.HashDataAsync(written, cancellationToken));
        return (sha256, written.Length, count);
    }
}
