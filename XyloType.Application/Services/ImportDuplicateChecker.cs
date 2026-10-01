using XyloType.Application.Interfaces;
using XyloType.Application.Models;
using XyloType.Domain.Entities;
using XyloType.Domain.Text;

namespace XyloType.Application.Services;

public class ImportDuplicateChecker : IImportDuplicateChecker
{
    private readonly IImportedSourceRepository _sourceRepository;
    private readonly IContentHasher _hasher;

    public ImportDuplicateChecker(
        IImportedSourceRepository sourceRepository,
        IContentHasher hasher)
    {
        _sourceRepository = sourceRepository;
        _hasher = hasher;
    }

    public async Task<ImportCheckResult> CheckAsync(string filePath, CancellationToken cancellationToken = default)
    {
        string title = Path.GetFileNameWithoutExtension(filePath);
        string hash = await _hasher.HashTextFileAsync(filePath, cancellationToken);

        List<ImportedSource> history = await _sourceRepository.GetAllAsync();

        ImportedSource? sameContent = history.FirstOrDefault(s => s.ContentHash == hash);

        List<ImportedSource> closeTitles =
            [.. history.Where(s => s != sameContent && TitleSimilarity.AreClose(title, s.Title))];

        return new ImportCheckResult(title, hash, sameContent, closeTitles);
    }
}
