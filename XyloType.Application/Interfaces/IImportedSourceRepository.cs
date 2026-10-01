using XyloType.Domain.Entities;

namespace XyloType.Application.Interfaces;

/// <summary>
/// History of the imported texts.
/// </summary>
public interface IImportedSourceRepository
{
    /// <summary>
    /// Most recent first.
    /// </summary>
    Task<List<ImportedSource>> GetAllAsync();
}
