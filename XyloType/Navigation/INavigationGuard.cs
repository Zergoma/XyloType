namespace XyloType.Navigation;

/// <summary>
/// A view that may keep the user from leaving it (e.g. unsaved changes).
/// </summary>
public interface INavigationGuard
{
    /// <summary>
    /// True when the view can be left (the user may be asked).
    /// </summary>
    Task<bool> CanLeaveAsync();
}
