using XyloType.Application.Interfaces;
using XyloType.Application.Models;
using XyloType.Domain.Entities;

namespace XyloType.Application.Services;

public class ImportedWordsGenerator : IImportedWordsGenerator
{
    private readonly IDactyloRepository _repository;
    private readonly IGetNextInRange _random;

    public ImportedWordsGenerator(
        IDactyloRepository repository,
        IGetNextInRange random)
    {
        _repository = repository;
        _random = random;
    }

    public async Task<Result<List<string>>> GenerateAsync(ImportedWordsOptions options, int count)
    {
        WordQueryBuilder query =
            new WordQueryBuilder()
            .WithLayout(options.Layout)
            .WithMinLength(Math.Min(options.MinLength, options.MaxLength))
            .WithMaxLength(Math.Max(options.MinLength, options.MaxLength));

        string[] languages = [.. options.Languages.Where(l => !string.IsNullOrWhiteSpace(l))];
        if (languages.Length > 0)
            query.WithLanguages(languages);

        List<Word> candidates = await _repository.SearchAsync(query.Build());

        // words are stored lower case: compare with the lower case allowed letters
        HashSet<char> allowed = [.. options.AllowedLetters.ToLowerInvariant()];
        List<Word> words = [.. candidates.Where(w => w.Text.All(allowed.Contains))];

        if (words.Count == 0)
        {
            return Result<List<string>>
                .Fail("Aucun mot importé ne correspond (langue, longueur, lettres). " +
                      "Importez des mots depuis le menu Import ou élargissez les lettres autorisées.");
        }

        WeightedPicker picker = new(words, _random);

        return Result<List<string>>
            .Ok([.. Enumerable.Range(0, Math.Max(0, count)).Select(_ => picker.Next())]);
    }

    /// <summary>
    /// Random pick weighted by the square root of the occurrence count:
    /// frequent words come up more often without hiding the rare ones.
    /// </summary>
    private sealed class WeightedPicker
    {
        private readonly List<Word> _words;
        private readonly int[] _cumulative;
        private readonly IGetNextInRange _random;

        public WeightedPicker(List<Word> words, IGetNextInRange random)
        {
            _words = words;
            _random = random;
            _cumulative = new int[words.Count];

            int total = 0;
            for (int i = 0; i < words.Count; i++)
            {
                total += Math.Max(1, (int)Math.Round(Math.Sqrt(words[i].OccurrenceCount)));
                _cumulative[i] = total;
            }
        }

        public string Next()
        {
            int ticket = _random.GetNext(0, _cumulative[^1]);
            int index = Array.BinarySearch(_cumulative, ticket + 1);
            if (index < 0)
                index = ~index;

            return _words[index].Text;
        }
    }
}
