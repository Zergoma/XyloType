using FluentAssertions;

using NSubstitute;

using XyloType.Application.Interfaces;
using XyloType.Application.Interfaces.Typing;
using XyloType.Application.Models;
using XyloType.Application.Services;
using XyloType.ViewModels.Typing;

namespace XyloType.Tests.ViewModels;

public class TypingViewModelInstrumentTests
{
    private static (TypingViewModel Vm, IUserTypingPreferenceService Preferences) Create(
        InstrumentChoice choice,
        params InstrumentChoice[] disabled)
    {
        IUserTypingPreferenceService preferences = Substitute.For<IUserTypingPreferenceService>();
        preferences.GetInstrument().Returns(choice);
        preferences.GetDisabledInstruments().Returns(disabled.ToHashSet());

        TypingViewModel vm = new(
            Substitute.For<IInputCharMapperService>(),
            Substitute.For<ITypingThemeProvider>(),
            Substitute.For<IThemeChangerService>(),
            Substitute.For<IPlaySoundSample>(),
            preferences,
            new ScoreCatalog());

        return (vm, preferences);
    }

    [Fact]
    public void ChangeInstrument_WithAChosenInstrument_GoesToTheNextEnabledOne()
    {
        var (vm, preferences) = Create(InstrumentChoice.Xylophone, InstrumentChoice.XylophoneRecorded);

        vm.ChangeInstrument();

        // the recorded xylophone is disabled: the piano comes next
        vm.CurrentInstrumentLabel.Should().Be("Piano");
        preferences.Received().SetInstrument(InstrumentChoice.Piano);
    }

    [Fact]
    public void ChangeInstrument_AtRandom_PicksAnotherInstrument()
    {
        var (vm, _) = Create(InstrumentChoice.Random);
        string before = vm.CurrentInstrumentLabel;

        vm.ChangeInstrument();

        vm.CurrentInstrumentLabel.Should().NotBe(before);
    }

    [Fact]
    public void ExcludeCurrentInstrument_SwitchesToRandom_AndNeverPlaysItAgain()
    {
        var (vm, preferences) = Create(InstrumentChoice.Piano);

        vm.ExcludeCurrentInstrument();

        vm.IsRandomInstrument.Should().BeTrue();
        vm.CurrentInstrumentLabel.Should().NotBe("Piano");
        vm.InstrumentSwitches.Single(s => s.Value == InstrumentChoice.Piano).IsEnabled.Should().BeFalse();
        preferences.Received().SetDisabledInstruments(Arg.Is<IEnumerable<InstrumentChoice>>(d => d.Contains(InstrumentChoice.Piano)));

        for (int i = 0; i < 20; i++)
        {
            vm.ChangeInstrument();
            vm.CurrentInstrumentLabel.Should().NotBe("Piano");
        }
    }

    [Fact]
    public void PreviousInstrument_GoesBackToTheInstrumentPlayedBefore()
    {
        var (vm, _) = Create(InstrumentChoice.Marimba);
        vm.HasPreviousInstrument.Should().BeFalse();

        vm.ChangeInstrument();
        vm.CurrentInstrumentLabel.Should().Be("Vibraphone");
        vm.HasPreviousInstrument.Should().BeTrue();

        vm.PreviousInstrument();

        vm.CurrentInstrumentLabel.Should().Be("Marimba");
        vm.InstrumentSelected.Value.Should().Be(InstrumentChoice.Marimba);
        vm.HasPreviousInstrument.Should().BeFalse();
    }

    [Fact]
    public void PreviousScore_GoesBackToThePiecePlayedBefore()
    {
        var (vm, _) = Create(InstrumentChoice.Xylophone);
        vm.OkSoundModeSelected = vm.OkSoundModeOptions.Single(o => o.Value == OkSoundMode.Instrumental);
        string first = vm.CurrentScoreTitle;
        vm.HasPreviousScore.Should().BeFalse();

        vm.SkipScore();
        vm.CurrentScoreTitle.Should().NotBe(first);
        vm.HasPreviousScore.Should().BeTrue();

        vm.PreviousScore();

        vm.CurrentScoreTitle.Should().Be(first);
        vm.HasPreviousScore.Should().BeFalse();
    }

    [Fact]
    public void PreviousScore_SkipsThePiecesExcludedSince()
    {
        var (vm, _) = Create(InstrumentChoice.Xylophone);
        vm.OkSoundModeSelected = vm.OkSoundModeOptions.Single(o => o.Value == OkSoundMode.Instrumental);
        string first = vm.CurrentScoreTitle;
        vm.SkipScore();

        vm.ScoreOptions.Single(o => o.Title == first).IsEnabled = false;

        vm.HasPreviousScore.Should().BeFalse();
    }

    [Fact]
    public void WithoutShuffle_ThePiecesFollowTheCatalogOrder()
    {
        var (vm, _) = Create(InstrumentChoice.Xylophone);
        vm.IsScoreShuffle.Should().BeFalse();
        vm.OkSoundModeSelected = vm.OkSoundModeOptions.Single(o => o.Value == OkSoundMode.Instrumental);

        // instrumental mode: the instrumental pieces only, songs left out
        string[] titles = [.. new ScoreCatalog().GetAll().Where(s => !s.IsSong).Select(s => s.Title)];
        vm.CurrentScoreTitle.Should().Be(titles[0]);

        vm.SkipScore();
        vm.CurrentScoreTitle.Should().Be(titles[1]);

        // a disabled piece is skipped
        vm.ScoreOptions.Where(o => !o.IsSong).ElementAt(2).IsEnabled = false;
        vm.SkipScore();
        vm.CurrentScoreTitle.Should().Be(titles[3]);
    }

    [Fact]
    public void ToggleScoreShuffle_IsSaved()
    {
        var (vm, preferences) = Create(InstrumentChoice.Xylophone);

        vm.ToggleScoreShuffle();

        vm.IsScoreShuffle.Should().BeTrue();
        preferences.Received().SetScoreShuffle(true);
    }

    [Fact]
    public void RandomInstrument_ChangesWithEachPiece()
    {
        var (vm, _) = Create(InstrumentChoice.Random);
        vm.OkSoundModeSelected = vm.OkSoundModeOptions.Single(o => o.Value == OkSoundMode.Instrumental);

        for (int i = 0; i < 10; i++)
        {
            string before = vm.CurrentInstrumentLabel;
            vm.SkipScore();
            vm.CurrentInstrumentLabel.Should().NotBe(before);
        }
    }

    [Fact]
    public void ToggleRandomInstrument_Off_KeepsTheCurrentInstrument()
    {
        var (vm, preferences) = Create(InstrumentChoice.Random);
        string current = vm.CurrentInstrumentLabel;

        vm.ToggleRandomInstrument();

        vm.IsRandomInstrument.Should().BeFalse();
        vm.CurrentInstrumentLabel.Should().Be(current);
        preferences.Received().SetInstrument(Arg.Is<InstrumentChoice>(i => i != InstrumentChoice.Random));

        vm.ToggleRandomInstrument();
        vm.IsRandomInstrument.Should().BeTrue();
    }

    [Fact]
    public void TheLastEnabledInstrument_CannotBeExcluded()
    {
        var (vm, _) = Create(
            InstrumentChoice.Random,
            InstrumentChoice.Xylophone, InstrumentChoice.XylophoneRecorded, InstrumentChoice.Piano,
            InstrumentChoice.Marimba, InstrumentChoice.Vibraphone);

        vm.CurrentInstrumentLabel.Should().Be("Glockenspiel");

        vm.ExcludeCurrentInstrument();
        vm.InstrumentSwitches.Single(s => s.Value == InstrumentChoice.Glockenspiel).IsEnabled = false;

        vm.CurrentInstrumentLabel.Should().Be("Glockenspiel");
        vm.InstrumentSwitches.Single(s => s.Value == InstrumentChoice.Glockenspiel).IsEnabled.Should().BeTrue();
    }

    [Fact]
    public void SongMode_PlaysSongsOnly_AndListsThemOnly()
    {
        var (vm, _) = Create(InstrumentChoice.Xylophone);
        vm.OkSoundModeSelected = vm.OkSoundModeOptions.Single(o => o.Value == OkSoundMode.Song);

        for (int i = 0; i < 10; i++)
        {
            vm.ScoreOptions.Single(o => o.Title == vm.CurrentScoreTitle).IsSong.Should().BeTrue();
            vm.SkipScore();
        }

        vm.ScoreCategoryGroups.SelectMany(g => g.Pieces).Should().OnlyContain(p => p.IsSong);
        vm.ScoreCategoryGroups.Should().NotContain(g => g.Name == "Ragtime");
    }
}
