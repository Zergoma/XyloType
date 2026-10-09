using FluentAssertions;

using Microsoft.Extensions.Logging.Abstractions;

using NSubstitute;

using XyloType.Application;
using XyloType.Application.DTOs;
using XyloType.Application.Interfaces;
using XyloType.Application.Models;
using XyloType.Application.Orchestrators;
using XyloType.Domain.Entities;

namespace XyloType.Tests.Application;

public class StarterPacksOrchestratorTests
{
    private static readonly KeyBoardLayoutDto s_azerty = new(KeyboardLayoutEnumDto.AzertyFr, "Azerty");

    private static ExercisePackInfo ExercisePack(string id, string layout = "AzertyFr")
        => new(id, id, "Débutant", "", layout, "1", id + ".json", "", 0, 3);

    private static WordPackInfo WordPack(string language)
        => new(language, "Mots " + language, "1", $"words-{language}.tsv.gz", "", 0, 100, []);

    private readonly IExercisePackSource _exerciseSource = Substitute.For<IExercisePackSource>();
    private readonly IExercisePackImporter _exerciseImporter = Substitute.For<IExercisePackImporter>();
    private readonly IWordPackSource _wordSource = Substitute.For<IWordPackSource>();
    private readonly IDactyloRepository _words = Substitute.For<IDactyloRepository>();

    public StarterPacksOrchestratorTests()
    {
        _exerciseSource.GetCatalogAsync(Arg.Any<CancellationToken>()).Returns(Result<ExercisePackCatalog>.Ok(new ExercisePackCatalog(1,
            [ExercisePack("azerty-a"), ExercisePack("azerty-b"), ExercisePack("bepo-a", "Bepo")])));
        _exerciseImporter.GetImportedVersionsAsync(Arg.Any<KeyBoardLayoutDto>())
            .Returns(new Dictionary<string, string> { ["azerty-b"] = "1" });
        _wordSource.GetCatalogAsync(Arg.Any<CancellationToken>()).Returns(Result<WordPackCatalog>.Ok(new WordPackCatalog(1,
            [WordPack("en"), WordPack("fr")])));
    }

    private void WordCount(int count)
        => _words.SearchPageAsync(Arg.Any<WordSearchCriteria>(), Arg.Any<WordSort>(), Arg.Any<int>(), Arg.Any<int>())
            .Returns(new WordSearchPage(new List<Word>(), count));

    private StarterPacksOrchestrator Create()
        => new(_exerciseSource, _exerciseImporter, _wordSource, Substitute.For<IWordImportOrchestrator>(),
            Substitute.For<IKeyboardKeyLocatorManager>(), _words, NullLogger<StarterPacksOrchestrator>.Instance);

    [Fact]
    public async Task Offer_HasTheMissingExercisePacksOfTheKeyboard_AndTheWordsOfItsLanguage_WhenTheDictionaryIsEmpty()
    {
        WordCount(0);

        StarterPacksOffer offer = await Create().GetOfferAsync(s_azerty);

        offer.ExercisePacks.Select(p => p.Id).Should().Equal("azerty-a");
        offer.WordPack!.LanguageCode.Should().Be("fr");
        offer.CatalogsAvailable.Should().BeTrue();
    }

    [Fact]
    public async Task Offer_HasNoWordPack_WhenTheDictionaryHasWords()
    {
        WordCount(12);

        StarterPacksOffer offer = await Create().GetOfferAsync(s_azerty);

        offer.WordPack.Should().BeNull();
        await _wordSource.DidNotReceive().GetCatalogAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Offer_WithoutConnection_SaysTheCatalogsWereNotAvailable()
    {
        WordCount(12);
        _exerciseSource.GetCatalogAsync(Arg.Any<CancellationToken>()).Returns(Result<ExercisePackCatalog>.Fail("offline"));

        StarterPacksOffer offer = await Create().GetOfferAsync(s_azerty);

        offer.IsEmpty.Should().BeTrue();
        offer.CatalogsAvailable.Should().BeFalse();
    }

    [Fact]
    public async Task Install_KeepsGoingAfterAPackThatFails()
    {
        WordCount(12);
        ExercisePackInfo broken = ExercisePack("broken"), fine = ExercisePack("fine");
        ExercisePack content = new(1, "fine", "Fine", "Débutant", "", "AzertyFr", "1", []);
        _exerciseSource.DownloadAsync(broken, Arg.Any<CancellationToken>()).Returns(Result<ExercisePack>.Fail("network"));
        _exerciseSource.DownloadAsync(fine, Arg.Any<CancellationToken>()).Returns(Result<ExercisePack>.Ok(content));
        _exerciseImporter.ImportAsync(content, s_azerty).Returns(Result<ExercisePackImportSummary>.Ok(new ExercisePackImportSummary(5, 0)));

        StarterPacksSummary summary = await Create().InstallAsync(new StarterPacksOffer(s_azerty, null, [broken, fine], true));

        summary.Exercises.Should().Be(5);
        summary.Errors.Should().ContainSingle().Which.Should().Contain("network");
    }
}
