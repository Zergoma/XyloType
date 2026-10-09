using System.Security.Cryptography;

using Microsoft.Extensions.Logging;

using XyloType.Application;
using XyloType.Application.Interfaces;
using XyloType.Application.Models;

namespace XyloType.Infrastructure.IO;

/// <summary>
/// The word packs published as the assets of the "words" release of the GitHub repository
/// (tools/publish-word-packs.ps1 updates them): a fixed address, whatever the releases of the app.
/// </summary>
public sealed class GitHubWordPackSource : IWordPackSource
{
    public static readonly string BaseUrl = GitHubReleases.AssetsUrl("words");

    private static HttpClient s_http => GitHubReleases.Http;

    private readonly ILogger<GitHubWordPackSource> _logger;

    public GitHubWordPackSource(ILogger<GitHubWordPackSource> logger)
    {
        _logger = logger;
    }

    public async Task<Result<WordPackCatalog>> GetCatalogAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            string json = await s_http.GetStringAsync(BaseUrl + WordPackCatalog.FileName, cancellationToken);
            WordPackCatalog? catalog = WordPackCatalog.FromJson(json);

            if (catalog is null)
                return Result<WordPackCatalog>.Fail("La liste des packs de mots est illisible.");

            if (catalog.Format > WordPackCatalog.CurrentFormat)
                return Result<WordPackCatalog>.Fail("Ces packs de mots demandent une version plus récente de XyloType.");

            return Result<WordPackCatalog>.Ok(catalog);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Word pack catalog not available");
            return Result<WordPackCatalog>.Fail("Impossible de récupérer la liste des packs de mots (connexion Internet ?).");
        }
    }

    public async Task<Result<string>> DownloadAsync(
        WordPackInfo pack,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default)
    {
        string folder = Path.Combine(Path.GetTempPath(), "XyloType", "word-packs");
        Directory.CreateDirectory(folder);
        string path = Path.Combine(folder, Path.GetFileName(pack.FileName));

        try
        {
            using HttpResponseMessage response = await s_http.GetAsync(
                BaseUrl + pack.FileName, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            response.EnsureSuccessStatusCode();

            long total = response.Content.Headers.ContentLength ?? pack.Size;

            // the checksum is computed while writing: no second read of the file
            using IncrementalHash sha256 = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
            await using (Stream download = await response.Content.ReadAsStreamAsync(cancellationToken))
            await using (FileStream file = File.Create(path))
            {
                byte[] buffer = new byte[81920];
                long received = 0;
                int read;
                while ((read = await download.ReadAsync(buffer, cancellationToken)) > 0)
                {
                    await file.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
                    sha256.AppendData(buffer, 0, read);
                    received += read;
                    if (total > 0)
                        progress?.Report(Math.Min(1.0, (double)received / total));
                }
            }

            string hash = Convert.ToHexStringLower(sha256.GetHashAndReset());
            if (!string.Equals(hash, pack.Sha256, StringComparison.OrdinalIgnoreCase))
            {
                File.Delete(path);
                _logger.LogWarning("Word pack {File} corrupted: {Hash} instead of {Expected}", pack.FileName, hash, pack.Sha256);
                return Result<string>.Fail("Le pack téléchargé est abîmé (somme de contrôle différente) : réessayez.");
            }

            progress?.Report(1.0);
            return Result<string>.Ok(path);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            TryDelete(path);
            throw;
        }
        catch (Exception ex)
        {
            TryDelete(path);
            _logger.LogWarning(ex, "Word pack {File} download failed", pack.FileName);
            return Result<string>.Fail("Le téléchargement du pack a échoué (connexion Internet ?).");
        }
    }

    private static void TryDelete(string path)
    {
        try { File.Delete(path); } catch (IOException) { } catch (UnauthorizedAccessException) { }
    }
}
