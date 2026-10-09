using XyloType.Application.Interfaces;

namespace XyloType.Infrastructure.IO;

/// <summary>
/// The assets of the app (sounds, scores, themes) as files of a folder next to the program.
/// </summary>
public sealed class FileAssetReader : IAssetReader
{
    private readonly string _root;

    /// <param name="root">The folder of the assets; their paths are relative to it, with '/' or '\'</param>
    public FileAssetReader(string root)
    {
        _root = root;
    }

    public Task<Stream> OpenAsync(string path)
    {
        string full = Path.GetFullPath(Path.Combine(_root, path.Replace('/', Path.DirectorySeparatorChar)));

        // an asset never comes from outside its folder
        if (!full.StartsWith(Path.GetFullPath(_root), StringComparison.OrdinalIgnoreCase))
            throw new UnauthorizedAccessException($"Asset outside of its folder: {path}");

        return Task.FromResult<Stream>(File.OpenRead(full));
    }
}
