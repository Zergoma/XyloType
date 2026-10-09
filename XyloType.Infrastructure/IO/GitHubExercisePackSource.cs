using System.Security.Cryptography;
using System.Text;

using Microsoft.Extensions.Logging;

using XyloType.Application;
using XyloType.Application.Interfaces;
using XyloType.Application.Models;

namespace XyloType.Infrastructure.IO;

/// <summary>
/// The exercise packs published as the assets of the "exercises" release of the GitHub repository
/// (tools/publish-exercise-packs.ps1 updates them from the packs/exercises folder).
/// </summary>
public sealed class GitHubExercisePackSource : IExercisePackSource
{
    public static readonly string BaseUrl =
#if DEBUG
        // packs being written can be tried before they are published: a local folder served over HTTP
        Environment.GetEnvironmentVariable("XYLOTYPE_EXERCISE_PACKS_URL") is { Length: > 0 } url ? url.TrimEnd('/') + "/" :
#endif
        GitHubReleases.AssetsUrl("exercises");

    private readonly ILogger<GitHubExercisePackSource> _logger;

    public GitHubExercisePackSource(ILogger<GitHubExercisePackSource> logger)
    {
        _logger = logger;
    }

    public async Task<Result<ExercisePackCatalog>> GetCatalogAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            string json = await GitHubReleases.Http.GetStringAsync(BaseUrl + ExercisePackCatalog.FileName, cancellationToken);
            ExercisePackCatalog? catalog = ExercisePackCatalog.FromJson(json);

            if (catalog is null)
                return Result<ExercisePackCatalog>.Fail("La liste des packs d'exercices est illisible.");

            if (catalog.Format > ExercisePackCatalog.CurrentFormat)
                return Result<ExercisePackCatalog>.Fail("Ces packs d'exercices demandent une version plus récente de XyloType.");

            return Result<ExercisePackCatalog>.Ok(catalog);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Exercise pack catalog not available");
            return Result<ExercisePackCatalog>.Fail("Impossible de récupérer la liste des packs d'exercices (connexion Internet ?).");
        }
    }

    public async Task<Result<ExercisePack>> DownloadAsync(ExercisePackInfo pack, CancellationToken cancellationToken = default)
    {
        try
        {
            // a few kilobytes: read at once
            byte[] content = await GitHubReleases.Http.GetByteArrayAsync(BaseUrl + pack.FileName, cancellationToken);

            string hash = Convert.ToHexStringLower(SHA256.HashData(content));
            if (!string.Equals(hash, pack.Sha256, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Exercise pack {File} corrupted: {Hash} instead of {Expected}", pack.FileName, hash, pack.Sha256);
                return Result<ExercisePack>.Fail("Le pack téléchargé est abîmé (somme de contrôle différente) : réessayez.");
            }

            ExercisePack? exercisePack = ExercisePack.FromJson(Encoding.UTF8.GetString(content));
            if (exercisePack is null || exercisePack.Format > ExercisePackCatalog.CurrentFormat)
                return Result<ExercisePack>.Fail("Ce pack d'exercices est illisible, ou demande une version plus récente de XyloType.");

            return Result<ExercisePack>.Ok(exercisePack);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Exercise pack {File} download failed", pack.FileName);
            return Result<ExercisePack>.Fail("Le téléchargement du pack a échoué (connexion Internet ?).");
        }
    }
}
