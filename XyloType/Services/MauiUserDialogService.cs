using XyloType.Application.Interfaces;

namespace XyloType.Services;

public class MauiUserDialogService : IUserDialogService
{
    // the single page of the app
    private static Page? Page
        => Microsoft.Maui.Controls.Application.Current?.Windows.FirstOrDefault()?.Page;

    public async Task<bool> ConfirmAsync(string title, string message, string accept, string cancel)
    {
        if (Page is not Page page)
            return false;

        return await page.DisplayAlertAsync(title, message, accept, cancel);
    }

    public async Task AlertAsync(string title, string message)
    {
        if (Page is not Page page)
            return;

        await page.DisplayAlertAsync(title, message, "OK");
    }

    public async Task<string?> PromptAsync(string title, string message, string accept, string cancel, string initialValue, int maxLength)
    {
        if (Page is not Page page)
            return null;

        return await page.DisplayPromptAsync(title, message, accept, cancel, maxLength: maxLength, initialValue: initialValue);
    }
}
