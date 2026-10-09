namespace XyloType.Application.Interfaces;

public interface IUserDialogService
{
    /// <summary>
    /// Asks the user a yes/no question.
    /// </summary>
    /// <returns>true if the user accepted</returns>
    Task<bool> ConfirmAsync(string title, string message, string accept, string cancel);

    /// <summary>
    /// Shows an information or error message.
    /// </summary>
    Task AlertAsync(string title, string message);

    /// <summary>
    /// Asks the user for a text.
    /// </summary>
    /// <returns>The text entered, null if the user cancelled</returns>
    Task<string?> PromptAsync(string title, string message, string accept, string cancel, string initialValue, int maxLength);
}
