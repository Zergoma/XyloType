using XyloType.Application.Interfaces;
using XyloType.Application.Models;
using XyloType.Domain.Entities;
using XyloType.Domain.Models;

namespace XyloType.Application.Services;

public sealed class KeyboardAnalyzerService : IKeyboardAnalyzerService
{
    public Result<UnitTextAnalysis> Analyze(
        string text,
        IReadOnlyDictionary<char, KeyInfo> map)
    {
        KeyboardRow rowMask = KeyboardRow.None;
        Finger fingerMask = Finger.None;
        bool externalAccent = false;

        int leftCount = 0;
        int rightCount = 0;

        foreach (char c in text)
        {
            if (!map.TryGetValue(c, out KeyInfo info))
            {
                // character not in the map
                // this word is not possible with that layout
                return Result<UnitTextAnalysis>.Fail($"The character {c} have no correspondance");
            }

            rowMask |= info.Row;
            fingerMask |= info.Finger;

            // one dead key in the word is enough
            externalAccent |= info.ExtrenalAccent;

            // a character can need both hands (e.g. Shift + dead key + letter)
            if ((info.Finger & LeftHand) != 0)
                leftCount++;

            if ((info.Finger & RightHand) != 0)
                rightCount++;
        }

        return Result<UnitTextAnalysis>.Ok(new ()
        {
            RowMask = rowMask,
            FingerMask = fingerMask,
            ExternalAccent = externalAccent,
            UsesLeftHand = leftCount > 0,
            UsesRightHand = rightCount > 0
        });
    }

    private const Finger LeftHand =
        Finger.LeftPinky | Finger.LeftRing | Finger.LeftMiddle | Finger.LeftIndex | Finger.LeftThumb;

    private const Finger RightHand =
        Finger.RightPinky | Finger.RightRing | Finger.RightMiddle | Finger.RightIndex | Finger.RightThumb;
}