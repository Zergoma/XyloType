namespace XyloType.Domain.Music;

/// <summary>
/// Note names to MIDI numbers: "C4" = 60 (middle C), "A4" = 69 (440 Hz).
/// Sharps with "#", flats with "b": "F#5", "Bb4".
/// </summary>
public static class Notes
{
    private static readonly Dictionary<char, int> s_semitones = new()
    {
        ['C'] = 0, ['D'] = 2, ['E'] = 4, ['F'] = 5, ['G'] = 7, ['A'] = 9, ['B'] = 11,
    };

    public static int ToMidi(string note)
    {
        if (note.Length < 2 || !s_semitones.TryGetValue(char.ToUpperInvariant(note[0]), out int semitone))
            throw new FormatException($"Invalid note \"{note}\"");

        int index = 1;
        if (note[index] == '#')
        {
            semitone++;
            index++;
        }
        else if (note[index] == 'b')
        {
            semitone--;
            index++;
        }

        if (!int.TryParse(note[index..], out int octave))
            throw new FormatException($"Invalid octave in note \"{note}\"");

        return (octave + 1) * 12 + semitone;
    }

    /// <summary>
    /// Parses a melody written as space separated note names: "E5 E5 F5 G5".
    /// </summary>
    public static int[] ParseMelody(string melody)
        => [.. melody
            .Split((char[])[' ', '\n', '\r', '\t', '|'], StringSplitOptions.RemoveEmptyEntries)
            .Select(ToMidi)];
}
