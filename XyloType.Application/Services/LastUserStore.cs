using XyloType.Application.Interfaces;

namespace XyloType.Application.Services;

/// <summary>
/// The user of the last session, in the settings store of the app.
/// </summary>
public class LastUserStore : ILastUserStore
{
    private const string LastUserIdKey = "last_user_id";

    private readonly ISettingsStore _store;

    public LastUserStore(ISettingsStore store)
    {
        _store = store;
    }

    public int? GetLastUserId()
        => _store.Contains(LastUserIdKey) ? _store.Get(LastUserIdKey, 0) : null;

    public void SetLastUserId(int? userId)
    {
        if (userId is int id)
            _store.Set(LastUserIdKey, id);
        else
            _store.Remove(LastUserIdKey);
    }
}
