namespace XyloType.Application.Interfaces;

public interface IUserDialogService
{
    /// <summary>
    /// Asks the user a yes/no question.
    /// </summary>
    /// <returns>true if the user accepted</returns>
    Task<bool> ConfirmAsync(string title, string message, string accept, string cancel);
}
