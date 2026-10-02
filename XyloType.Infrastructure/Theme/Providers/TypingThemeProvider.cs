using XyloType.Application;
using XyloType.Application.Interfaces.Typing;
using XyloType.Application.Models.Themes;
using XyloType.Infrastructure.Theme.Loaders;
using XyloType.Infrastructure.Theme.Mappers;
using XyloType.Infrastructure.Theme.Models;

namespace XyloType.Infrastructure.Theme.Providers;

public class TypingThemeProvider : ITypingThemeProvider
{
    private readonly AssetThemesLoader _assetThemesLoader;
    private readonly UserThemesLoader _userThemesLoader;
    private readonly Dictionary<string, ITypingTheme> _themes = [];
    private readonly System.Collections.Concurrent.ConcurrentDictionary<string, ThemeFileModel> _files = new();
    public TypingThemeProvider(
        AssetThemesLoader themeLoader,
        UserThemesLoader userThemesLoader)
    {
        _assetThemesLoader = themeLoader;
        _userThemesLoader = userThemesLoader;
    }
    public bool ContainsTheme(string name)
    {
        return _themes.ContainsKey(name);
    }

    public async Task<Result<ITypingTheme>> GetThemeAsync(string name, ThemeState themeState, CancellationToken cancellationToken = default)
    {
        // the theme file is read once (a user theme first, else the one of the app);
        // only its light or dark version is made again
        if (!_files.TryGetValue(name, out ThemeFileModel? file))
        {
            Result<ThemeFileModel> loaded = await LoadFileAsync(name);
            if (!loaded.Success)
                return Result<ITypingTheme>.Fail(loaded.Error);

            file = loaded.GetValue;
            _files[name] = file;
        }

        ITypingTheme theme = file.ToTheme(themeState);
        _themes[name] = theme;
        return Result<ITypingTheme>.Ok(theme);
    }

    private async Task<Result<ThemeFileModel>> LoadFileAsync(string name)
    {
        Result<ThemeFileModel> userResult = await _userThemesLoader.LoadAsync(name);
        if (userResult.Success)
            return userResult;

        Result<ThemeFileModel> assetResult = await _assetThemesLoader.LoadAsync(name);
        return assetResult.Success
            ? assetResult
            : Result<ThemeFileModel>.Fail($"Theme: {name} doesn't exist");
    }
}
