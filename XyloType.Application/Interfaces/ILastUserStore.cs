namespace XyloType.Application.Interfaces;

/// <summary>
/// Remembers the user of the last session: the app starts with it.
/// </summary>
public interface ILastUserStore
{
    int? GetLastUserId();

    void SetLastUserId(int? userId);
}
