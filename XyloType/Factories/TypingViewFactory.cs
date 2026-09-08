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




    public TypingViewFactory(
        ITypingThemeProvider typingThemeProvider,
        IInputCharMapperService charMapper,
        IThemeChangerService themeChangerService,
        ILogger<TypingView> logger,
        IPlaySoundSample soundSamplePlayer)
    {
        _typingThemeProvider = typingThemeProvider;
        _charMapper = charMapper;
        _themeChangerService = themeChangerService;
        _soundSamplePlayer = soundSamplePlayer;
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
                _soundSamplePlayer);

        await typingviewmodel.LoadTextAsync(stringProvider);

        TypingView typingView =
            new(
                typingviewmodel,
                navigationService);

        return Result<ContentPage>
            .Ok(typingView);
    }
}
