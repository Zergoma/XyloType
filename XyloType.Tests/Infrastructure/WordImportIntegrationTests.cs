using FluentAssertions;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

using NSubstitute;

using XyloType.Application;
using XyloType.Application.Interfaces;
using XyloType.Application.Models.Typing.Exercices;
using XyloType.Application.Models;
using XyloType.Application.Orchestrators;
using XyloType.Application.Services;
using XyloType.Domain.Entities;
using XyloType.Domain.Enums;
using XyloType.Infrastructure.DbContexts;
using XyloType.Infrastructure.IO;
using XyloType.Infrastructure.Repositories;

namespace XyloType.Tests.Infrastructure;

/// <summary>
/// Full word import on a real SQLite database (temporary file) created by the migrations.
/// </summary>
public sealed class WordImportIntegrationTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "xylotype-tests", Guid.NewGuid().ToString("N"));
    private readonly TestDbContextFactory _factory;

    public WordImportIntegrationTests()
    {
        Directory.CreateDirectory(_folder);
        _factory = new TestDbContextFactory(Path.Combine(_folder, "dactylo.db3"));

        using DactyloDbContext ctx = _factory.CreateDbContext();
        ctx.Database.Migrate();
    }

    public void Dispose()
    {
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        try { Directory.Delete(_folder, recursive: true); } catch (IOException) { }
    }

    private string WriteText(string content)
    {
        string path = Path.Combine(_folder, $"{Guid.NewGuid():N}.txt");
        File.WriteAllText(path, content);
        return path;
    }

    private WordImportOrchestrator CreateOrchestrator()
        => new(
            new DactyloRepository(_factory, NullLogger<DactyloRepository>.Instance),
            new WordBatchProcessorOrchestrator(new KeyboardAnalyzerService()),
            new TextFileWordReader(),
            new NormalizedTextHasher(),
            NullLogger<WordImportOrchestrator>.Instance);

    private DactyloRepository CreateRepository()
        => new(_factory, NullLogger<DactyloRepository>.Instance);

    [Fact]
    public async Task Import_StoresWordsWithTheirAnalysis()
    {
        string file = WriteText("Le chat mange. Le CHAT dort, cœur !");

        Result<WordImportSummary> result =
            await CreateOrchestrator().ImportAsync(file, "fr", new AzertyKeysLocator());

        result.Success.Should().BeTrue(result.Error);
        WordImportSummary summary = result.GetValue;
        summary.WordsRead.Should().Be(7);
        summary.NewWords.Should().Be(4);        // le, chat, mange, dort
        summary.UpdatedWords.Should().Be(0);
        summary.IgnoredWords.Should().Be(1);    // cœur: œ is not on the keyboard
        summary.IgnoredSample.Should().Equal("cœur");

        using DactyloDbContext ctx = _factory.CreateDbContext();
        Word chat = await ctx.Words.Include(w => w.Analyses).SingleAsync(w => w.Text == "chat");
        chat.OccurrenceCount.Should().Be(2, "the case is ignored");
        chat.LanguageCode.Should().Be("fr");
        chat.Analyses.Should().ContainSingle(a => a.Layout == KeyboardLayout.AzertyFr);
    }

    [Fact]
    public async Task Import_TwiceTheSameFile_UpdatesCountsWithoutDuplicates()
    {
        string file = WriteText("chat chien chat");
        WordImportOrchestrator orchestrator = CreateOrchestrator();
        await orchestrator.ImportAsync(file, "fr", new AzertyKeysLocator());

        Result<WordImportSummary> second =
            await CreateOrchestrator().ImportAsync(file, "fr", new AzertyKeysLocator());

        second.Success.Should().BeTrue(second.Error);
        second.GetValue.NewWords.Should().Be(0);
        second.GetValue.UpdatedWords.Should().Be(2);

        using DactyloDbContext ctx = _factory.CreateDbContext();
        (await ctx.Words.CountAsync()).Should().Be(2);
        (await ctx.WordAnalyses.CountAsync()).Should().Be(2, "one analysis per word and layout");
        (await ctx.Words.SingleAsync(w => w.Text == "chat")).OccurrenceCount.Should().Be(4);
    }

    [Fact]
    public async Task WordsExercise_UsesOnlyImportedWordsMadeOfAllowedLetters()
    {
        // Arrange: "zebre" uses letters outside "chatlesi", "le" is too short
        string file = WriteText("le chat lit les listes, ciel zebre");
        await CreateOrchestrator().ImportAsync(file, "fr", new AzertyKeysLocator());

        ITypingExerciseLineNumberService lines = Substitute.For<ITypingExerciseLineNumberService>();
        lines.LineNumber.Returns(3);
        ITypingExerciseWordNumberService words = Substitute.For<ITypingExerciseWordNumberService>();
        words.ItemNumber.Returns(5);

        TypingExerciseDynamicWordsProducer producer = new(
            new ImportedWordsGenerator(
                new DactyloRepository(_factory, NullLogger<DactyloRepository>.Instance),
                new SequenceRandom()),
            words,
            lines,
            new TypingTextDataDynamic { LengthMin = 3, LengthMax = 6, LanguagesSelected = ["fr"] },
            "chatlesi",
            KeyboardLayout.AzertyFr);

        // Act
        Result<IEnumerable<string>> result = await producer.GetStringsAsync();

        // Assert
        result.Success.Should().BeTrue(result.Error);
        string[] generatedLines = [.. result.GetValue];
        generatedLines.Should().HaveCount(3);
        generatedLines
            .SelectMany(l => l.Split(' '))
            .Should().HaveCount(15)
            .And.OnlyContain(w => w == "chat" || w == "lit" || w == "les" || w == "listes" || w == "ciel");
    }

    [Fact]
    public async Task WordsExercise_WithoutMatchingWord_FailsWithAMessage()
    {
        TypingExerciseDynamicWordsProducer producer = new(
            new ImportedWordsGenerator(
                new DactyloRepository(_factory, NullLogger<DactyloRepository>.Instance),
                new SequenceRandom()),
            Substitute.For<ITypingExerciseWordNumberService>(),
            Substitute.For<ITypingExerciseLineNumberService>(),
            new TypingTextDataDynamic { LengthMin = 3, LengthMax = 6, LanguagesSelected = ["fr"] },
            "abc",
            KeyboardLayout.AzertyFr);

        Result<IEnumerable<string>> result = await producer.GetStringsAsync();

        result.Success.Should().BeFalse();
        result.Error.Should().Contain("Aucun mot importé");
    }

    /// <summary>
    /// Deterministic "random": walks through the range.
    /// </summary>
    private sealed class SequenceRandom : IGetNextInRange
    {
        private int _next;
        public int GetNext(int min, int max) => min + (_next++ % (max - min));
    }

    [Fact]
    public async Task Import_ReportsProgressUpToTheWholeFile()
    {
        string file = WriteText(string.Join(Environment.NewLine, Enumerable.Repeat("le chat dort sur le tapis", 300)));
        List<WordImportProgress> reports = [];

        await CreateOrchestrator().ImportAsync(file, "fr", new AzertyKeysLocator(), new SyncProgress<WordImportProgress>(reports.Add));

        reports.Should().NotBeEmpty();
        reports.Where(r => r.Phase == WordImportPhase.Reading).Select(r => r.Fraction).Should().BeInAscendingOrder();
        reports[0].Fraction.Should().Be(0);
        reports.Should().Contain(r => r.Phase == WordImportPhase.Reading);
        reports.Where(r => r.Phase == WordImportPhase.Saving).Select(r => r.Fraction)
            .Should().NotBeEmpty().And.BeInAscendingOrder().And.EndWith(1);
        reports[^1].Should().Be(new WordImportProgress(WordImportPhase.Saving, 1800, 1));
    }

    private sealed class SyncProgress<T>(Action<T> report) : IProgress<T>
    {
        public void Report(T value) => report(value);
    }

    [Fact]
    public async Task Import_IsRecordedInTheHistory_AndDetectedAsDuplicate()
    {
        string file = Path.Combine(_folder, "Les Misérables.txt");
        File.WriteAllText(file, "le chat dort\r\nsur le tapis");
        await CreateOrchestrator().ImportAsync(file, "fr", new AzertyKeysLocator());

        ImportDuplicateChecker checker = new(new ImportedSourceRepository(_factory), new NormalizedTextHasher());

        // same text, other encoding and line endings
        string copy = Path.Combine(_folder, "autre nom.txt");
        File.WriteAllText(copy, "le chat dort  \nsur le tapis\n\n", new System.Text.UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
        ImportCheckResult sameContent = await checker.CheckAsync(copy);

        // other text, close title
        string edition = Path.Combine(_folder, "les miserables (1).txt");
        File.WriteAllText(edition, "une autre histoire");
        ImportCheckResult closeTitle = await checker.CheckAsync(edition);

        sameContent.SameContent.Should().NotBeNull();
        sameContent.SameContent!.Title.Should().Be("Les Misérables");
        sameContent.SameContent.WordsRead.Should().Be(6);

        closeTitle.SameContent.Should().BeNull();
        closeTitle.CloseTitles.Should().ContainSingle(s => s.Title == "Les Misérables");
    }

    [Fact]
    public async Task ExcludedWord_StaysExcludedOnReimport_AndIsNeverGenerated()
    {
        string file = WriteText("chat chat chien");
        await CreateOrchestrator().ImportAsync(file, "fr", new AzertyKeysLocator());

        DactyloRepository repository = CreateRepository();
        Word chat = (await repository.SearchAsync(new WordQueryBuilder().WithText("chat").Build())).Single();
        await repository.SetExcludedAsync(chat.Id, true);

        await CreateOrchestrator().ImportAsync(file, "fr", new AzertyKeysLocator());

        using (DactyloDbContext ctx = _factory.CreateDbContext())
        {
            Word stored = await ctx.Words.SingleAsync(w => w.Text == "chat");
            stored.IsExcluded.Should().BeTrue();
            stored.OccurrenceCount.Should().Be(4);
        }

        Result<List<string>> generated = await new ImportedWordsGenerator(repository, new SequenceRandom())
            .GenerateAsync(new ImportedWordsOptions(["fr"], "chaiten", 3, 6, KeyboardLayout.AzertyFr), 10);

        generated.GetValue.Should().OnlyContain(w => w == "chien");
    }

    [Fact]
    public async Task SearchPage_FiltersSortsAndCounts()
    {
        await CreateOrchestrator().ImportAsync(
            WriteText("le le le chat chat chien vert caresse fête"), "fr", new AzertyKeysLocator());
        DactyloRepository repository = CreateRepository();

        WordSearchPage byFrequency = await repository.SearchPageAsync(new WordSearchCriteria(), WordSort.Default, 0, 2);
        byFrequency.TotalCount.Should().Be(6);
        byFrequency.Words.Select(w => w.Text).Should().Equal("le", "chat");

        WordSearchPage onlyLetters = await repository.SearchPageAsync(
            new WordQueryBuilder().WithOnlyLetters("chatien").Build(), new WordSort(WordSortField.Text, false), 0, 50);
        onlyLetters.Words.Select(w => w.Text).Should().Equal("chat", "chien");

        WordSearchPage contains = await repository.SearchPageAsync(
            new WordQueryBuilder().WithText("CH").WithMinOccurrences(2).Build(), new WordSort(WordSortField.Text, false), 0, 50);
        contains.Words.Select(w => w.Text).Should().Equal("chat");

        WordSearchPage occurrenceRange = await repository.SearchPageAsync(
            new WordQueryBuilder().WithMinOccurrences(2).WithMaxOccurrences(2).Build(), new WordSort(WordSortField.Text, false), 0, 50);
        occurrenceRange.Words.Select(w => w.Text).Should().Equal("chat");

        WordSearchPage longestFirst = await repository.SearchPageAsync(
            new WordSearchCriteria(), new WordSort(WordSortField.Length, Descending: true), 0, 1);
        longestFirst.Words.Single().Text.Should().Be("caresse");

        WordSearchPage leftHand = await repository.SearchPageAsync(
            new WordQueryBuilder().WithLayout(KeyboardLayout.AzertyFr).WithHands(HandFilter.LeftOnly).Build(), new WordSort(WordSortField.Text, false), 0, 50);
        leftHand.Words.Select(w => w.Text).Should().Equal("caresse", "vert");
    }

    [Fact]
    public async Task SearchPage_SortsByHandsAndExclusion()
    {
        await CreateOrchestrator().ImportAsync(
            WriteText("le le le chat chat chien vert caresse fête"), "fr", new AzertyKeysLocator());
        DactyloRepository repository = CreateRepository();
        WordSearchCriteria azerty = new WordQueryBuilder().WithLayout(KeyboardLayout.AzertyFr).Build();

        // left hand only first
        WordSearchPage byHands = await repository.SearchPageAsync(azerty, new WordSort(WordSortField.Hands, false), 0, 2);
        byHands.Words.Select(w => w.Text).Should().Equal("caresse", "vert");

        WordSearchPage byHandsDescending = await repository.SearchPageAsync(azerty, new WordSort(WordSortField.Hands, true), 0, 50);
        byHandsDescending.Words.TakeLast(2).Select(w => w.Text).Should().Equal("caresse", "vert");

        int vert = byHands.Words.Single(w => w.Text == "vert").Id;
        await repository.SetExcludedAsync(vert, excluded: true);

        WordSearchPage excludedFirst = await repository.SearchPageAsync(
            new WordSearchCriteria { Exclusion = WordExclusionFilter.All }, new WordSort(WordSortField.Excluded, true), 0, 50);
        excludedFirst.Words.First().Text.Should().Be("vert");
        excludedFirst.Words.Skip(1).Should().OnlyContain(w => !w.IsExcluded);
    }

    [Fact]
    public async Task Import_CancelledWhileReading_WritesNothing()
    {
        // big enough to report progress several times
        string file = WriteText(string.Join(Environment.NewLine,
            Enumerable.Range(0, 3000).Select(i => $"le chat numéro dort mange joue court saute")));
        using CancellationTokenSource cancellation = new();

        // cancel as soon as a part of the file has been read
        SyncProgress<WordImportProgress> progress = new(p =>
        {
            if (p.Fraction is > 0.2 and < 1)
                cancellation.Cancel();
        });

        Result<WordImportSummary> result =
            await CreateOrchestrator().ImportAsync(file, "fr", new AzertyKeysLocator(), progress, cancellation.Token);

        result.Success.Should().BeFalse();
        result.Error.Should().Contain("annulé");

        using DactyloDbContext ctx = _factory.CreateDbContext();
        (await ctx.Words.CountAsync()).Should().Be(0);
        (await ctx.WordAnalyses.CountAsync()).Should().Be(0);
        (await ctx.ImportedSources.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task Import_SpanningSeveralBatches_CountsEachWordOnce()
    {
        // more than one batch of distinct words, each one repeated in every batch
        IEnumerable<string> words = Enumerable.Range(0, 2500).Select(i => ToLetters(i));
        string text = string.Join(" ", words);
        string file = WriteText(text + Environment.NewLine + text);

        Result<WordImportSummary> result =
            await CreateOrchestrator().ImportAsync(file, "fr", new AzertyKeysLocator());

        result.GetValue.NewWords.Should().Be(2500);
        result.GetValue.UpdatedWords.Should().Be(0, "a word created by this import stays a new word");

        using DactyloDbContext ctx = _factory.CreateDbContext();
        (await ctx.Words.CountAsync()).Should().Be(2500);
        (await ctx.Words.AllAsync(w => w.OccurrenceCount == 2)).Should().BeTrue();
        (await ctx.ImportedSources.CountAsync()).Should().Be(1);
    }

    /// <summary>
    /// Distinct letter-only word for a number: 0 → "aa", 1 → "ab"...
    /// </summary>
    private static string ToLetters(int value)
    {
        const string letters = "abcdefghijklmnopqrstuvwxyz";
        string word = "";
        do
        {
            word = letters[value % 26] + word;
            value /= 26;
        }
        while (value > 0);
        return "w" + word;
    }

    [Fact]
    public async Task Import_MissingFile_FailsWithoutThrowing()
    {
        Result<WordImportSummary> result =
            await CreateOrchestrator().ImportAsync(Path.Combine(_folder, "missing.txt"), "fr", new AzertyKeysLocator());

        result.Success.Should().BeFalse();
    }

    private sealed class TestDbContextFactory(string dbPath) : IDbContextFactory<DactyloDbContext>
    {
        public DactyloDbContext CreateDbContext()
            => new(new DbContextOptionsBuilder<DactyloDbContext>()
                .UseSqlite($"Data Source={dbPath}")
                .Options);
    }
}
