using Microsoft.Extensions.Logging;

using XyloType.Application.Interfaces;
using XyloType.Domain.Entities;

namespace XyloType.Application.Services;

public class CurrentUserService : ICurrentUserService
{
    private readonly IUserRepository _repository;
    private readonly ILastUserStore _lastUserStore;
    private readonly TimeProvider _time;
    private readonly ILogger<CurrentUserService> _logger;

    private List<UserProfile> _users = [];

    public CurrentUserService(
        IUserRepository repository,
        ILastUserStore lastUserStore,
        TimeProvider time,
        ILogger<CurrentUserService> logger)
    {
        _repository = repository;
        _lastUserStore = lastUserStore;
        _time = time;
        _logger = logger;
    }

    public UserProfile? CurrentUser { get; private set; }

    public bool HasUser => CurrentUser is not null;

    public IReadOnlyList<UserProfile> Users => _users;

    public event EventHandler? Changed;

    public async Task InitializeAsync()
    {
        _users = await _repository.GetAllAsync();

        int? lastId = _lastUserStore.GetLastUserId();
        CurrentUser = _users.Find(u => u.Id == lastId) ?? _users.FirstOrDefault();
        _lastUserStore.SetLastUserId(CurrentUser?.Id);

        _logger.LogInformation("{Count} users, current: {UserId}", _users.Count, CurrentUser?.Id);
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public async Task<Result<UserProfile>> CreateAsync(string name)
    {
        Result<string> checkedName = CheckName(name, exceptUserId: null);
        if (!checkedName.Success)
            return Result<UserProfile>.Fail(checkedName.Error);

        UserProfile user = await _repository.AddAsync(checkedName.GetValue, _time.GetUtcNow().UtcDateTime);
        _users = await _repository.GetAllAsync();
        Select(_users.Find(u => u.Id == user.Id) ?? user);

        return Result<UserProfile>.Ok(CurrentUser!);
    }

    public async Task<Result<bool>> RenameAsync(int userId, string name)
    {
        Result<string> checkedName = CheckName(name, exceptUserId: userId);
        if (!checkedName.Success)
            return Result<bool>.Fail(checkedName.Error);

        if (_users.Find(u => u.Id == userId) is not UserProfile user)
            return Result<bool>.Fail("Cet utilisateur n'existe plus.");

        await _repository.RenameAsync(userId, checkedName.GetValue);
        user.Name = checkedName.GetValue;
        _users = [.. _users.OrderBy(u => u.Name, StringComparer.CurrentCultureIgnoreCase)];

        Changed?.Invoke(this, EventArgs.Empty);
        return Result<bool>.Ok(true);
    }

    public async Task<Result<bool>> DeleteAsync(int userId)
    {
        if (_users.Find(u => u.Id == userId) is not UserProfile user)
            return Result<bool>.Fail("Cet utilisateur n'existe plus.");

        await _repository.DeleteAsync(userId);
        _users.Remove(user);

        // the current user is gone: the first other one takes over, if any
        if (CurrentUser?.Id == userId)
            Select(_users.FirstOrDefault());
        else
            Changed?.Invoke(this, EventArgs.Empty);

        return Result<bool>.Ok(true);
    }

    public Result<bool> SwitchTo(int userId)
    {
        if (_users.Find(u => u.Id == userId) is not UserProfile user)
            return Result<bool>.Fail("Cet utilisateur n'existe plus.");

        if (CurrentUser?.Id != userId)
            Select(user);

        return Result<bool>.Ok(true);
    }

    private void Select(UserProfile? user)
    {
        CurrentUser = user;
        _lastUserStore.SetLastUserId(user?.Id);
        Changed?.Invoke(this, EventArgs.Empty);
    }

    /// <returns>The name trimmed, or why it cannot be used</returns>
    private Result<string> CheckName(string name, int? exceptUserId)
    {
        string trimmed = (name ?? string.Empty).Trim();

        if (trimmed.Length == 0)
            return Result<string>.Fail("Donnez un nom.");

        if (trimmed.Length > UserProfile.NameMaxLength)
            return Result<string>.Fail($"Le nom fait {UserProfile.NameMaxLength} caractères au plus.");

        if (_users.Any(u => u.Id != exceptUserId && string.Equals(u.Name, trimmed, StringComparison.CurrentCultureIgnoreCase)))
            return Result<string>.Fail($"« {trimmed} » existe déjà.");

        return Result<string>.Ok(trimmed);
    }
}
