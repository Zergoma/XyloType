using XyloType.Application.Interfaces;

namespace XyloType.Services;

public class MauiLastUserStore : ILastUserStore
{
    private const string LastUserIdKey = "last_user_id";

    public int? GetLastUserId()
        => Preferences.Default.ContainsKey(LastUserIdKey)
            ? Preferences.Default.Get(LastUserIdKey, 0)
            : null;

    public void SetLastUserId(int? userId)
    {
        if (userId is int id)
            Preferences.Default.Set(LastUserIdKey, id);
        else
            Preferences.Default.Remove(LastUserIdKey);
    }
}
