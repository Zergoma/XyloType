using CommunityToolkit.Mvvm.ComponentModel;

using XyloType.Application.Models;

namespace XyloType.ViewModels.Import;

/// <summary>
/// A word pack of the catalog, as shown in the import section.
/// </summary>
public partial class WordPackItem : ObservableObject
{
    public WordPackItem(WordPackInfo info)
    {
        Info = info;
    }

    public WordPackInfo Info { get; }

    public string Title => Info.Title;

    public string Details => $"{Info.WordCount:N0} mots · {Info.Size / 1024.0:N0} Ko · version {Info.Version}";

    public string SourcesText => Info.Sources.Count == 0 ? string.Empty : "Tiré de : " + string.Join(", ", Info.Sources);

    /// <summary>
    /// This very pack (same checksum) is already in the import history.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ActionText))]
    public partial bool IsImported { get; set; }

    public string ActionText => IsImported ? "Réimporter" : "Télécharger et importer";
}
