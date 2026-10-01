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
            NullLogger<WordImportOrchestrator>.Instance);

    [Fact]
    public async Task Import_StoresWordsWithTheirAnalysis()
    {
        string file = WriteText("Le chat mange. Le CHAT dort, cœur !");

        Result<WordImportSummary> result =
            await CreateOrchestrator().ImportAsync(file, "fr", new AzertyKeysLocator());

        result.Success.Should().BeTrue(result.Error);
        result.GetValue.Should().Be(new WordImportSummary(
            WordsRead: 7,
            NewWords: 4,        // le, chat, mange, dort
            UpdatedWords: 0,
            IgnoredWords: 1));  // cœur: œ is not on the keyboard

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
        reports.Select(r => r.Fraction).Should().BeInAscendingOrder();
        reports[0].Fraction.Should().Be(0);
        reports[^1].Should().Be(new WordImportProgress(1800, 1));
    }

    private sealed class SyncProgress<T>(Action<T> report) : IProgress<T>
    {
        public void Report(T value) => report(value);
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
