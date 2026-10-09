using XyloType.Application.Interfaces;

namespace XyloType.Application.Services;

/// <summary>
/// The keyboard chosen on the home page, in the settings store of the app.
/// </summary>
public class UserKeyboardLayoutPreferenceService : IUserKeyboardLayoutPreferenceService
{
    private const string SelectedKeyboardKey = "selected_keyboard";

    private readonly ISettingsStore _store;

    public UserKeyboardLayoutPreferenceService(ISettingsStore store)
    {
        _store = store;
    }

    public Result<int> GetKeyboardType()
        => _store.Contains(SelectedKeyboardKey)
            ? Result<int>.Ok(_store.Get(SelectedKeyboardKey, 0))
            : Result<int>.Fail("No keyboard chosen yet");

    public void SetKeyboardType(int keyBoardLayoutDtoId)
        => _store.Set(SelectedKeyboardKey, keyBoardLayoutDtoId);
}
