using XyloType.Domain.Entities;

namespace XyloType.Application.Models;

/// <summary>
/// What the import history says about a file about to be imported.
/// </summary>
/// <param name="Title">Title the file will get (its name without extension)</param>
/// <param name="ContentHash">Hash of its normalized text</param>
/// <param name="SameContent">A previous import of exactly the same text, if any</param>
/// <param name="CloseTitles">Previous imports with a close title (another edition, a copy...)</param>
public record ImportCheckResult(
    string Title,
    string ContentHash,
    ImportedSource? SameContent,
    IReadOnlyList<ImportedSource> CloseTitles);
