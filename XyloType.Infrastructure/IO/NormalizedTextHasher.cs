using System.Security.Cryptography;
using System.Text;

using XyloType.Application.Interfaces;

namespace XyloType.Infrastructure.IO;

public sealed class NormalizedTextHasher : IContentHasher
{
    public async Task<string> HashTextFileAsync(string filePath, CancellationToken cancellationToken = default)
    {
        using IncrementalHash sha = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);

        // the reader detects the encoding (BOM) and decodes to text
        using StreamReader reader = new(filePath, detectEncodingFromByteOrderMarks: true);

        string? line;
        while ((line = await reader.ReadLineAsync(cancellationToken)) is not null)
        {
            // one normalized line: same Unicode form, no trailing spaces, "\n" line ending
            string normalized = line.Normalize(NormalizationForm.FormC).TrimEnd();
            if (normalized.Length == 0)
                continue; // blank lines do not change the text

            sha.AppendData(Encoding.UTF8.GetBytes(normalized));
            sha.AppendData("\n"u8);
        }

        return Convert.ToHexStringLower(sha.GetHashAndReset());
    }
}
