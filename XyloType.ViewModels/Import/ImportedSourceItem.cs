using XyloType.Domain.Entities;

namespace XyloType.ViewModels.Import;

/// <summary>
/// One line of the import history.
/// </summary>
public class ImportedSourceItem
{
    public ImportedSourceItem(ImportedSource source)
    {
        Title = source.Title;
        Details =
            $"{source.ImportedAtUtc.ToLocalTime():g} · {source.LanguageCode} · " +
            $"{source.WordsRead:N0} mots lus · {source.NewWords:N0} nouveaux · {source.IgnoredWords:N0} impossibles à taper";
    }

    public string Title { get; }

    public string Details { get; }
}
