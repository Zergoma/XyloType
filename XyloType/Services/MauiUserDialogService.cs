using XyloType.Application.Interfaces;

namespace XyloType.Services;

public class MauiUserDialogService : IUserDialogService
{
    public async Task<bool> ConfirmAsync(string title, string message, string accept, string cancel)
    {
        Page? page = Shell.Current?.CurrentPage ?? Microsoft.Maui.Controls.Application.Current?.Windows.FirstOrDefault()?.Page;
        if (page is null)
            return false;

        return await page.DisplayAlertAsync(title, message, accept, cancel);
    }
}
