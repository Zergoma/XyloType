using FluentAssertions;

using NSubstitute;

using XyloType.Application;
using XyloType.Application.Interfaces;
using XyloType.Application.Interfaces.Typing;
using XyloType.Application.Models.Themes;
using XyloType.Application.Services;
using XyloType.ViewModels.Typing;

namespace XyloType.Tests.ViewModels;

public class TypingViewModelVisibleLinesTests
{
    private static async Task<TypingViewModel> CreateLoadedViewModel(int lineCount)
    {
        ITypingThemeProvider themeProvider = Substitute.For<ITypingThemeProvider>();
        themeProvider
            .GetThemeAsync(Arg.Any<string>(), Arg.Any<ThemeState>())
            .Returns(Result<ITypingTheme>.Ok(Substitute.For<ITypingTheme>()));

        IUserTypingPreferenceService preferences = Substitute.For<IUserTypingPreferenceService>();
        preferences.GetBackReturnEnable().Returns(true);

        IInputCharMapperService mapper = Substitute.For<IInputCharMapperService>();
        mapper.Map(Arg.Any<char>()).Returns(call => call.Arg<char>());

        TypingViewModel vm = new(
            mapper,
            themeProvider,
            Substitute.For<IThemeChangerService>(),
            Substitute.For<IPlaySoundSample>(),
            preferences,
            new ScoreCatalog());

        // one character per line: one key press moves to the next line
        IStringsProvider text = Substitute.For<IStringsProvider>();
        text.GetStringsAsync().Returns(Result<IEnumerable<string>>.Ok(Enumerable.Repeat("a", lineCount)));

        (await vm.LoadTextAsync(text)).Success.Should().BeTrue();
        return vm;
    }

    private static int[] VisibleLineNumbers(TypingViewModel vm)
        => [.. vm.VisibleLines.Select(l => vm.LinesStates.IndexOf(l))];

    [Fact]
    public async Task OnlyAWindowOfLines_IsShown()
    {
        TypingViewModel vm = await CreateLoadedViewModel(44);

        vm.LinesStates.Should().HaveCount(44);
        VisibleLineNumbers(vm).Should().Equal(0, 1, 2, 3, 4);
    }

    [Fact]
    public async Task Window_FollowsTheCurrentLine()
    {
        TypingViewModel vm = await CreateLoadedViewModel(44);

        for (int i = 0; i < 10; i++)
            vm.ProcessInput('a');

        // below the current line: updated at once; above: kept until the view has scrolled
        VisibleLineNumbers(vm).Should().StartWith([0, 1]).And.EndWith([13, 14]);
        vm.TopLinesToRemove(10).Should().Be(9);

        vm.SettleVisibleLines(10);

        // current line 10: the previous one and the 4 next ones
        VisibleLineNumbers(vm).Should().Equal(9, 10, 11, 12, 13, 14);
        vm.VisibleIndexOf(10).Should().Be(1);
        vm.VisibleIndexOf(30).Should().Be(-1);
    }

    [Fact]
    public async Task Window_GoesBack_WithBackspace()
    {
        TypingViewModel vm = await CreateLoadedViewModel(44);
        for (int i = 0; i < 10; i++)
            vm.ProcessInput('a');
        vm.SettleVisibleLines(10);

        vm.ProcessInput('\b');

        // the previous line comes back above once the view is ready to keep the text still
        vm.TopLinesToInsert(9).Should().Be(1);
        vm.SettleVisibleLines(9);

        VisibleLineNumbers(vm).Should().Equal(8, 9, 10, 11, 12, 13);
    }

    [Fact]
    public async Task Window_StopsAtTheLastLine()
    {
        TypingViewModel vm = await CreateLoadedViewModel(6);
        for (int i = 0; i < 5; i++)
            vm.ProcessInput('a');
        vm.SettleVisibleLines(5);

        VisibleLineNumbers(vm).Should().Equal(4, 5);
    }
}
