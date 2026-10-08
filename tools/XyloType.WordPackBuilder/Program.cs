using System.Globalization;

using Microsoft.Data.Sqlite;

using XyloType.Application.Models;
using XyloType.Infrastructure.IO;

// Builds the word packs from a XyloType database: one file per language with the words and their occurrences
// (the excluded words are left out), and the catalog listing them. Used by tools/publish-word-packs.ps1.
//
//   XyloType.WordPackBuilder --db <dactylo.db3> --out <folder> [--version 2026.10.08]

Dictionary<string, string> options = ParseOptions(args);
if (!options.TryGetValue("db", out string? dbPath) || !options.TryGetValue("out", out string? outFolder))
{
    Console.Error.WriteLine("Usage: XyloType.WordPackBuilder --db <dactylo.db3> --out <folder> [--version yyyy.MM.dd]");
    return 1;
}

if (!File.Exists(dbPath))
{
    Console.Error.WriteLine($"Database not found: {dbPath}");
    return 1;
}

string version = options.GetValueOrDefault("version") ?? DateTime.UtcNow.ToString("yyyy.MM.dd", CultureInfo.InvariantCulture);
Directory.CreateDirectory(outFolder);

// read only: the database of the app is never changed
await using SqliteConnection connection = new($"Data Source={dbPath};Mode=ReadOnly");
await connection.OpenAsync();

List<WordPackInfo> packs = [];
foreach (string language in await QueryAsync(connection,
             "SELECT DISTINCT LanguageCode FROM Words WHERE IsExcluded = 0 ORDER BY LanguageCode",
             r => r.GetString(0)))
{
    List<(string Text, int Occurrences)> words = await QueryAsync(connection,
        "SELECT Text, OccurrenceCount FROM Words WHERE LanguageCode = $language AND IsExcluded = 0 " +
        "ORDER BY OccurrenceCount DESC, Text",
        r => (r.GetString(0), r.GetInt32(1)),
        ("$language", language));

    // the texts the words come from, without the packs imported before (they come from the same texts)
    List<string> sources = await QueryAsync(connection,
        "SELECT DISTINCT Title FROM ImportedSources WHERE LanguageCode = $language AND FileName NOT LIKE 'words-%.tsv.gz' " +
        "ORDER BY Title",
        r => r.GetString(0),
        ("$language", language));

    string title = $"Mots {LanguageName(language)}";
    string fileName = WordPackFormat.FileNameFor(language);

    var (sha256, size, count) = await WordPackWriter.WriteAsync(
        Path.Combine(outFolder, fileName),
        words,
        [$"XyloType word pack: {title} {version}", $"Sources: {string.Join(", ", sources)}", "word<TAB>occurrences"]);

    packs.Add(new WordPackInfo(language, title, version, fileName, sha256, size, count, sources));
    Console.WriteLine($"{fileName}: {count:N0} words, {size / 1024.0:N0} KB");
}

WordPackCatalog catalog = new(WordPackCatalog.CurrentFormat, packs);
await File.WriteAllTextAsync(Path.Combine(outFolder, WordPackCatalog.FileName), catalog.ToJson());
Console.WriteLine($"{WordPackCatalog.FileName}: {packs.Count} pack(s), version {version}");
return 0;

static Dictionary<string, string> ParseOptions(string[] args)
{
    Dictionary<string, string> options = new(StringComparer.OrdinalIgnoreCase);
    for (int i = 0; i + 1 < args.Length; i += 2)
    {
        if (args[i].StartsWith("--", StringComparison.Ordinal))
            options[args[i][2..]] = args[i + 1];
    }

    return options;
}

static string LanguageName(string code) => code switch
{
    "fr" => "français",
    "en" => "anglais",
    "de" => "allemands",
    "es" => "espagnols",
    "it" => "italiens",
    "pt" => "portugais",
    _ => $"({code})",
};

static async Task<List<T>> QueryAsync<T>(
    SqliteConnection connection,
    string sql,
    Func<SqliteDataReader, T> map,
    params (string Name, object Value)[] parameters)
{
    await using SqliteCommand command = connection.CreateCommand();
    command.CommandText = sql;
    foreach (var (name, value) in parameters)
        command.Parameters.AddWithValue(name, value);

    List<T> rows = [];
    await using SqliteDataReader reader = await command.ExecuteReaderAsync();
    while (await reader.ReadAsync())
        rows.Add(map(reader));

    return rows;
}
