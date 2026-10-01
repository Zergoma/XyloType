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
}
