using Xylocopadream.UI.Avalonia.Dialogs;

using XyloType.Application;
using XyloType.Application.Interfaces;

namespace XyloType.Desktop.Services;

/// <summary>
/// Choice of a text file to import, with the file dialog of the system.
/// </summary>
public class AvaloniaChooseFilePresenter : IChoosePath
{
    private readonly IDialogService _dialogs;

    public AvaloniaChooseFilePresenter(IDialogService dialogs)
    {
        _dialogs = dialogs;
    }

    public async Task<Result<string?>> SelectPathAsync()
    {
        try
        {
            IReadOnlyList<string> files = await _dialogs.PickFilesAsync(
                "Choisir un texte",
                allowMultiple: false,
                [new FileFilter("Textes", "*.txt"), FileFilter.AllFiles]);

            // cancelled: no path
            return Result<string?>.Ok(files.FirstOrDefault());
        }
        catch (Exception ex)
        {
            return Result<string?>.Fail($"Error on file selection: {ex.Message}");
        }
    }
}
