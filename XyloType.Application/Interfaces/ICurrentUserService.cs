using XyloType.Domain.Entities;

namespace XyloType.Application.Interfaces;

/// <summary>
/// The users of the app and the one using it now (the user of the last session at start).
/// </summary>
public interface ICurrentUserService
{
    /// <summary>
    /// Null when there is no user yet.
    /// </summary>
    UserProfile? CurrentUser { get; }

    bool HasUser { get; }

    /// <summary>
    /// By name.
    /// </summary>
    IReadOnlyList<UserProfile> Users { get; }

    /// <summary>
    /// The current user or the list of the users changed.
    /// </summary>
    event EventHandler? Changed;

    /// <summary>
    /// Loads the users and selects the user of the last session (or the first one).
    /// </summary>
    Task InitializeAsync();

    /// <summary>
    /// Creates a user and makes it the current one.
    /// </summary>
    Task<Result<UserProfile>> CreateAsync(string name);

    Task<Result<bool>> RenameAsync(int userId, string name);

    /// <summary>
    /// Deletes a user and its results; another user becomes the current one if it was.
    /// </summary>
    Task<Result<bool>> DeleteAsync(int userId);

    Result<bool> SwitchTo(int userId);
}
