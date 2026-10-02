using FluentAssertions;

using NSubstitute;

using XyloType.Application.Interfaces;
using XyloType.Application.Models;
using XyloType.ViewModels.WordsExplorer;

namespace XyloType.Tests.ViewModels;

public class WordsExplorerViewModelSortTests
{
    private static WordsExplorerViewModel Create()
    {
        ILanguageAvailableService languages = Substitute.For<ILanguageAvailableService>();
        languages.GetAvailableLanguage().Returns(["fr", "en"]);

        return new WordsExplorerViewModel(
            Substitute.For<IDactyloRepository>(),
            Substitute.For<IUserKeyboardLayoutPreferenceService>(),
            Substitute.For<IKeyBoardLayoutAvailableService>(),
            languages);
    }

    [Fact]
    public void SortableHeaders_ShowAnIcon_TheSortedOneItsDirection()
    {
        WordsExplorerViewModel vm = Create();

        vm.OccurrencesHeader.Should().Be("OCCURRENCES ▼");
        vm.TextHeader.Should().Be("MOT ↕");
        vm.LanguageHeader.Should().Be("LANGUE ↕");
        vm.HandsHeader.Should().Be("MAIN(S) ↕");

        // only the active words are shown by default: nothing to sort
        vm.ExcludedHeader.Should().Be("EXCLU");
    }

    [Fact]
    public void ColumnFilteredToASingleValue_CannotBeSorted()
    {
        WordsExplorerViewModel vm = Create();
        vm.SortBy(nameof(WordSortField.Language));
        vm.LanguageHeader.Should().Be("LANGUE ▲");

        vm.LanguageSelected = vm.LanguageOptions.Single(o => o.Value == "fr");
        vm.HandsSelected = vm.HandsOptions.Single(o => o.Value == HandFilter.LeftOnly);

        vm.LanguageHeader.Should().Be("LANGUE");
        vm.HandsHeader.Should().Be("MAIN(S)");
        vm.Sort.Should().Be(WordSort.Default);

        vm.SortBy(nameof(WordSortField.Hands));
        vm.Sort.Should().Be(WordSort.Default);

        vm.ExclusionSelected = vm.ExclusionOptions.Single(o => o.Value == WordExclusionFilter.All);
        vm.SortBy(nameof(WordSortField.Excluded));
        vm.ExcludedHeader.Should().Be("EXCLU ▼");
    }
}
