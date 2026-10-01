using Microsoft.Extensions.Logging;

using XyloType.MVVM.Views;

using XyloType.Application;
using XyloType.Application.Interfaces;
using XyloType.Application.Interfaces.Typing;
using XyloType.ViewModels.Typing;

namespace XyloType.Factories;

public class TypingViewFactory : ITypingViewFactory
{
    private readonly ITypingThemeProvider _typingThemeProvider;
    private readonly IInputCharMapperService _charMapper;
    private readonly IThemeChangerService _themeChangerService;
    private readonly IPlaySoundSample _soundSamplePlayer;
    private readonly IUserTypingPreferenceService _typingPreference;
    private readonly IScoreCatalog _scoreCatalog;




    public TypingViewFactory(
        ITypingThemeProvider typingThemeProvider,
        IInputCharMapperService charMapper,
        IThemeChangerService themeChangerService,
        ILogger<TypingView> logger,
        IPlaySoundSample soundSamplePlayer,
        IUserTypingPreferenceService typingPreference,
        IScoreCatalog scoreCatalog)
    {
        _typingThemeProvider = typingThemeProvider;
        _charMapper = charMapper;
        _themeChangerService = themeChangerService;
        _soundSamplePlayer = soundSamplePlayer;
        _typingPreference = typingPreference;
        _scoreCatalog = scoreCatalog;
    }

    public async Task<Result<ContentPage>> CreateTypingViewAsync(
        IStringsProvider stringProvider,
        INavigationService navigationService)
    {
        TypingViewModel typingviewmodel =
            new(
                _charMapper,
                _typingThemeProvider,
                _themeChangerService,
                _soundSamplePlayer,
                _typingPreference,
                _scoreCatalog);

        Result<bool> loadResult = await typingviewmodel.LoadTextAsync(stringProvider);
        if (!loadResult.Success)
        {
            return Result<ContentPage>
                .Fail(loadResult.Error);
        }

        TypingView typingView =
            new(
                typingviewmodel,
                navigationService);

        return Result<ContentPage>
            .Ok(typingView);
    }
}
