using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;

using FluentAssertions;

using XyloType.Application.Models;
using XyloType.Infrastructure.IO;

namespace XyloType.Tests.Infrastructure;

public sealed class WordPackFormatTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "xylotype-tests", Guid.NewGuid().ToString("N"));

    public WordPackFormatTests() => Directory.CreateDirectory(_folder);

    public void Dispose()
    {
        try { Directory.Delete(_folder, recursive: true); } catch (IOException) { }
    }

    private static async Task<List<(string Text, int Occurrences)>> ReadAllAsync(string path, IProgress<double>? progress = null)
    {
        List<(string, int)> words = [];
        await foreach (var word in new WordPackReader().ReadAsync(path, progress))
            words.Add(word);
        return words;
    }

    [Fact]
    public async Task WrittenPack_IsReadBack_WithItsChecksum()
    {
        string path = Path.Combine(_folder, "words-fr.tsv.gz");
        (string, int)[] words = [("de", 23014), ("été", 120), ("aujourd'hui", 15), ("ôtez-moi", 1)];

        var (sha256, size, count) = await WordPackWriter.WriteAsync(path, words, ["XyloType word pack", "word<TAB>occurrences"]);

        count.Should().Be(4);
        size.Should().Be(new FileInfo(path).Length);
        sha256.Should().Be(Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(path))));
        (await ReadAllAsync(path)).Should().Equal(words);
    }

    [Fact]
    public async Task Reader_SkipsCommentsAndMalformedLines()
    {
        string path = Path.Combine(_folder, "words-fr.tsv.gz");
        await using (GZipStream zipped = new(File.Create(path), CompressionLevel.Fastest))
        await using (StreamWriter writer = new(zipped, Encoding.UTF8))
        {
            await writer.WriteAsync("# comment\nchat\t7\n\nno-count\nchien\tmany\ndeux mots\t3\nzero\t0\nmoins\t-2\n\t5\nrat\t2\n");
        }

        (await ReadAllAsync(path)).Should().Equal(("chat", 7), ("rat", 2));
    }

    [Fact]
    public async Task Reader_ReportsProgressUpToTheEnd()
    {
        string path = Path.Combine(_folder, "words-fr.tsv.gz");
        await WordPackWriter.WriteAsync(path, Enumerable.Range(0, 20_000).Select(i => ($"mot{i}", i + 1)), []);
        List<double> reports = [];

        await ReadAllAsync(path, new SyncProgress(reports.Add));

        reports.Should().NotBeEmpty().And.BeInAscendingOrder().And.EndWith(1.0);
    }

    [Fact]
    public void Catalog_GoesToJsonAndBack()
    {
        WordPackCatalog catalog = new(WordPackCatalog.CurrentFormat,
        [
            new WordPackInfo("fr", "Mots français", "2026.10.08", "words-fr.tsv.gz", "abc123", 105400, 27079, ["Madame Bovary"]),
        ]);

        string json = catalog.ToJson();
        WordPackCatalog? back = WordPackCatalog.FromJson(json);

        json.Should().Contain("\"languageCode\": \"fr\"").And.Contain("Mots français", "accents are written as is");
        back.Should().NotBeNull();
        back!.Format.Should().Be(1);
        back.Packs.Should().ContainSingle().Which.Should().BeEquivalentTo(catalog.Packs[0]);
    }

    private sealed class SyncProgress(Action<double> report) : IProgress<double>
    {
        public void Report(double value) => report(value);
    }
}
