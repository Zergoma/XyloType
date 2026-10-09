using Xylocopadream.UI.Avalonia.Dialogs;

using XyloType.Application.Interfaces;

namespace XyloType.Desktop.Services;

/// <summary>
/// The questions and messages of the view models, as the dialogs of Xylocopadream.UI over the main window.
/// </summary>
public class AvaloniaUserDialogService : IUserDialogService
{
    private readonly IDialogService _dialogs;

    public AvaloniaUserDialogService(IDialogService dialogs)
    {
        _dialogs = dialogs;
    }

    public async Task<bool> ConfirmAsync(string title, string message, string accept, string cancel)
        => await _dialogs.ChooseAsync(title, message, [accept, cancel], defaultIndex: 0, cancelIndex: 1) == 0;

    public Task AlertAsync(string title, string message)
        => _dialogs.ChooseAsync(title, message, ["OK"]);

    public async Task<string?> PromptAsync(string title, string message, string accept, string cancel, string initialValue, int maxLength)
    {
        string? text = await _dialogs.PromptAsync(title, message, initialValue, accept);
        return text is null ? null : text.Length > maxLength ? text[..maxLength] : text;
    }
}
