namespace XyloType.Domain.Music;

/// <summary>
/// Plays a score one note at a time, within the range of the instrument.
/// The score is transposed to fit (by octaves when possible, otherwise in another key), keeping its intervals;
/// only a melody wider than the range gets its extreme notes folded back by octaves.
/// When the score ends, it starts again.
/// </summary>
public class Melody
{
    private readonly int[] _notes;
    private int _next;

    public Melody(Score score, int lowestNote, int highestNote)
    {
        if (score.Notes.Count == 0)
            throw new ArgumentException("The score has no note", nameof(score));

        if (highestNote - lowestNote < 12)
            throw new ArgumentException("The range must cover at least one octave");

        Score = score;
        int shift = Transposition(score.Notes, lowestNote, highestNote);
        _notes = [.. score.Notes.Select(n => Fold(n + shift, lowestNote, highestNote))];
    }

    public Score Score { get; }

    /// <summary>
    /// Notes actually played (after transposition).
    /// </summary>
    public IReadOnlyList<int> Notes => _notes;

    /// <summary>
    /// True before the first note and right after the last one: the score is complete.
    /// </summary>
    public bool IsAtStart => _next == 0;

    /// <summary>
    /// The next note of the melody, looping at the end.
    /// </summary>
    public int NextNote()
    {
        int note = _notes[_next];
        _next = (_next + 1) % _notes.Length;
        return note;
    }

    /// <summary>
    /// Semitones to add so the whole melody fits the range, keeping every interval:
    /// whole octaves when possible (same key), otherwise the smallest change of key.
    /// A melody wider than the range is centered (its extreme notes are folded afterwards).
    /// </summary>
    private static int Transposition(IReadOnlyList<int> notes, int lowestNote, int highestNote)
    {
        int min = notes.Min();
        int max = notes.Max();

        // every shift in [minShift, maxShift] keeps all the notes in range
        int minShift = lowestNote - min;
        int maxShift = highestNote - max;

        if (minShift > maxShift)
        {
            double melodyCenter = (min + max) / 2.0;
            double rangeCenter = (lowestNote + highestNote) / 2.0;
            return (int)Math.Round((rangeCenter - melodyCenter) / 12.0) * 12;
        }

        // same key: a whole number of octaves, the closest to the original pitch
        int firstOctave = (int)Math.Ceiling(minShift / 12.0) * 12;
        if (firstOctave <= maxShift)
        {
            int octaveShift = firstOctave;
            for (int shift = firstOctave; shift <= maxShift; shift += 12)
            {
                if (Math.Abs(shift) < Math.Abs(octaveShift))
                    octaveShift = shift;
            }
            return octaveShift;
        }

        // other key: the smallest shift
        return Math.Abs(minShift) <= Math.Abs(maxShift) ? minShift : maxShift;
    }

    private static int Fold(int note, int lowestNote, int highestNote)
    {
        while (note < lowestNote)
            note += 12;
        while (note > highestNote)
            note -= 12;
        return note;
    }
}
