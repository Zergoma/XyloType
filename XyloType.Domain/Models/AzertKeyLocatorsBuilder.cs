using XyloType.Domain.Entities;

namespace XyloType.Domain.Models;

/// <summary>
/// French AZERTY layout, with the standard touch typing finger assignment.
/// Row A: number row (&amp; é " ' ( - è _ ç à), B: AZERTYUIOP, C: QSDFGHJKLM, D: WXCVBN.
/// A character typed with several keys (dead key accents) combines the rows and fingers
/// of every key pressed and is flagged <see cref="KeyInfo.ExtrenalAccent"/>.
/// </summary>
public class AzertKeyLocatorsBuilder
{
    // ^ dead key, right of P, typed with the right pinky
    private static readonly KeyInfo s_circumflexKey = new(KeyboardRow.B, Finger.RightPinky);

    // ¨ = Shift (left pinky, the dead key being on the right side) + ^ key
    private static readonly KeyInfo s_diaeresisKeys = new(KeyboardRow.B, Finger.RightPinky | Finger.LeftPinky);

    public static Dictionary<char, KeyInfo> BuildMap()
    {
        Dictionary<char, KeyInfo> map = new()
        {
            // =========================
            // ROW B (AZERTYUIOP)
            // =========================

            ['a'] = new KeyInfo(KeyboardRow.B, Finger.LeftPinky),
            ['z'] = new KeyInfo(KeyboardRow.B, Finger.LeftRing),
            ['e'] = new KeyInfo(KeyboardRow.B, Finger.LeftMiddle),
            ['r'] = new KeyInfo(KeyboardRow.B, Finger.LeftIndex),
            ['t'] = new KeyInfo(KeyboardRow.B, Finger.LeftIndex),

            ['y'] = new KeyInfo(KeyboardRow.B, Finger.RightIndex),
            ['u'] = new KeyInfo(KeyboardRow.B, Finger.RightIndex),
            ['i'] = new KeyInfo(KeyboardRow.B, Finger.RightMiddle),
            ['o'] = new KeyInfo(KeyboardRow.B, Finger.RightRing),
            ['p'] = new KeyInfo(KeyboardRow.B, Finger.RightPinky),

            // =========================
            // ROW C (QSDFGHJKLM ù)
            // =========================

            ['q'] = new KeyInfo(KeyboardRow.C, Finger.LeftPinky),
            ['s'] = new KeyInfo(KeyboardRow.C, Finger.LeftRing),
            ['d'] = new KeyInfo(KeyboardRow.C, Finger.LeftMiddle),
            ['f'] = new KeyInfo(KeyboardRow.C, Finger.LeftIndex),
            ['g'] = new KeyInfo(KeyboardRow.C, Finger.LeftIndex),

            ['h'] = new KeyInfo(KeyboardRow.C, Finger.RightIndex),
            ['j'] = new KeyInfo(KeyboardRow.C, Finger.RightIndex),
            ['k'] = new KeyInfo(KeyboardRow.C, Finger.RightMiddle),
            ['l'] = new KeyInfo(KeyboardRow.C, Finger.RightRing),
            ['m'] = new KeyInfo(KeyboardRow.C, Finger.RightPinky),
            ['ù'] = new KeyInfo(KeyboardRow.C, Finger.RightPinky),

            // =========================
            // ROW D (WXCVBN)
            // =========================

            ['w'] = new KeyInfo(KeyboardRow.D, Finger.LeftPinky),
            ['x'] = new KeyInfo(KeyboardRow.D, Finger.LeftRing),
            ['c'] = new KeyInfo(KeyboardRow.D, Finger.LeftMiddle),
            ['v'] = new KeyInfo(KeyboardRow.D, Finger.LeftIndex),
            ['b'] = new KeyInfo(KeyboardRow.D, Finger.LeftIndex),

            ['n'] = new KeyInfo(KeyboardRow.D, Finger.RightIndex),

            // =========================
            // ROW A (number row, unshifted: & é " ' ( - è _ ç à)
            // =========================

            ['é'] = new KeyInfo(KeyboardRow.A, Finger.LeftRing),     // key 2
            ['\''] = new KeyInfo(KeyboardRow.A, Finger.LeftIndex),   // key 4
            ['-'] = new KeyInfo(KeyboardRow.A, Finger.RightIndex),   // key 6
            ['è'] = new KeyInfo(KeyboardRow.A, Finger.RightIndex),   // key 7
            ['ç'] = new KeyInfo(KeyboardRow.A, Finger.RightRing),    // key 9
            ['à'] = new KeyInfo(KeyboardRow.A, Finger.RightPinky),   // key 0

            // digits need Shift on AZERTY, kept for completeness
            ['1'] = new KeyInfo(KeyboardRow.A, Finger.LeftPinky),
            ['2'] = new KeyInfo(KeyboardRow.A, Finger.LeftRing),
            ['3'] = new KeyInfo(KeyboardRow.A, Finger.LeftMiddle),
            ['4'] = new KeyInfo(KeyboardRow.A, Finger.LeftIndex),
            ['5'] = new KeyInfo(KeyboardRow.A, Finger.LeftIndex),

            ['6'] = new KeyInfo(KeyboardRow.A, Finger.RightIndex),
            ['7'] = new KeyInfo(KeyboardRow.A, Finger.RightIndex),
            ['8'] = new KeyInfo(KeyboardRow.A, Finger.RightMiddle),
            ['9'] = new KeyInfo(KeyboardRow.A, Finger.RightRing),
            ['0'] = new KeyInfo(KeyboardRow.A, Finger.RightPinky),

            // =========================
            // SPACE
            // =========================

            [' '] = new KeyInfo(KeyboardRow.None, Finger.LeftThumb | Finger.RightThumb),
        };

        // =========================
        // DEAD KEY ACCENTS: dead key, then the vowel
        // =========================

        AddDeadKey(map, 'â', 'a', s_circumflexKey);
        AddDeadKey(map, 'ê', 'e', s_circumflexKey);
        AddDeadKey(map, 'î', 'i', s_circumflexKey);
        AddDeadKey(map, 'ô', 'o', s_circumflexKey);
        AddDeadKey(map, 'û', 'u', s_circumflexKey);

        AddDeadKey(map, 'ë', 'e', s_diaeresisKeys);
        AddDeadKey(map, 'ï', 'i', s_diaeresisKeys);
        AddDeadKey(map, 'ü', 'u', s_diaeresisKeys);
        AddDeadKey(map, 'ÿ', 'y', s_diaeresisKeys);

        return map;
    }

    private static void AddDeadKey(Dictionary<char, KeyInfo> map, char accented, char baseLetter, KeyInfo deadKey)
    {
        KeyInfo letter = map[baseLetter];
        map[accented] = new KeyInfo(
            deadKey.Row | letter.Row,
            deadKey.Finger | letter.Finger,
            ExtrenalAccent: true);
    }
}
