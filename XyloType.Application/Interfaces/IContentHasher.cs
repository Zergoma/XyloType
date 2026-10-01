namespace XyloType.Application.Interfaces;

public interface IContentHasher
{
    /// <summary>
    /// SHA-256 (hex) of the normalized text of a file: the same text gives the same hash
    /// whatever its encoding, line endings or trailing spaces.
    /// </summary>
    Task<string> HashTextFileAsync(string filePath, CancellationToken cancellationToken = default);
}
