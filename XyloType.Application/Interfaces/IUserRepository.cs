using XyloType.Domain.Entities;

namespace XyloType.Application.Interfaces;

/// <summary>
/// The users of the app, stored in the database.
/// </summary>
public interface IUserRepository
{
    /// <summary>
    /// By name.
    /// </summary>
    Task<List<UserProfile>> GetAllAsync();

    Task<UserProfile> AddAsync(string name, DateTime createdAtUtc);

    Task RenameAsync(int userId, string name);

    /// <summary>
    /// Deletes the user and the results of its exercises.
    /// </summary>
    Task DeleteAsync(int userId);
}
