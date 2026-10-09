using XyloType.Application.Models;

namespace XyloType.Application.Interfaces;

/// <summary>
/// Where the exercise packs are published (a release of the GitHub repository).
/// </summary>
public interface IExercisePackSource
{
    Task<Result<ExercisePackCatalog>> GetCatalogAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Downloads a pack, checked against its checksum.
    /// </summary>
    Task<Result<ExercisePack>> DownloadAsync(ExercisePackInfo pack, CancellationToken cancellationToken = default);
}
