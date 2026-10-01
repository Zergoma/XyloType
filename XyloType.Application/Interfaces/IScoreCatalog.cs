using XyloType.Domain.Music;

namespace XyloType.Application.Interfaces;

/// <summary>
/// Pieces of music played note by note while typing.
/// </summary>
public interface IScoreCatalog
{
    IReadOnlyList<Score> GetAll();
}
