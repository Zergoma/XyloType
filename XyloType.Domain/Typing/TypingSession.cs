using System.Diagnostics;

using XyloType.Domain.Enums;
using XyloType.Domain.Typing.Analysis;

namespace XyloType.Domain.Typing;

public class TypingSession
{
    public event Action<int>? LineChanged;
    public event Action<HitKeyStatus>? HitKeyStatusChanged;

    private TypingChar? _previousCurrent;
    public List<TypingLine> Lines { get; set; } = [];

    public int CurrentLineIndex { get; private set; } = 0;

    public int CurrentCharacterIndex { get; private set; } = 0;

    public bool BackReturnEnable { get; set; } = true;
    public bool StopOnError { get; set; } = true;

    // Time spent on the current character
    private readonly Stopwatch _stopwatch = new();

    // Time of the whole session, from the first key press to the last character
    private readonly Stopwatch _sessionStopwatch = new();

    public TimeSpan Duration => _sessionStopwatch.Elapsed;

    /// <summary>
    /// Part of the text already typed, from 0 to 1.
    /// </summary>
    public double Progress
    {
        get
        {
            int total = Lines.Sum(l => l.Characters.Count);
            if (total == 0)
                return 0;

            if (_isEnded)
                return 1;

            int typed = Lines.Take(CurrentLineIndex).Sum(l => l.Characters.Count) + CurrentCharacterIndex;
            return Math.Clamp((double)typed / total, 0, 1);
        }
    }

    private bool _isFirstChar = true;
    private bool _isEnded;
    private bool _isPaused;

    private void SetPosition(int lineIndex, int characterIndex, bool forceRefresh = false)
    {
        int lineIdxSecured = Math.Max(lineIndex, 0);
        int charIdxSecured = Math.Max(characterIndex, 0);

        bool lineDelta = CurrentLineIndex != lineIdxSecured;
        bool columnDelta = CurrentCharacterIndex != charIdxSecured;

        if (lineDelta)
        {
            CurrentLineIndex = lineIdxSecured;
            LineChanged?.Invoke(CurrentLineIndex);
        }

        if (columnDelta)
        {
            CurrentCharacterIndex = charIdxSecured;
        }

        if (forceRefresh || lineDelta || columnDelta)
        {
            UpdateCurrent();
        }
    }

    public void ResetProgression()
    {
        // ResetProgression Character state and error
        for (int i = 0; i < Lines.Count; i++)
        {
            TypingLine line = Lines[i];

            foreach(TypingChar letter in  line.Characters)
            {
                letter.Reset();
            }
        }

        _previousCurrent = null;
        SetPosition(0, 0, true);
        _isFirstChar = true;
        _isEnded = false;
        _isPaused = false;
        _stopwatch.Reset();
        _sessionStopwatch.Reset();
    }


    private void UpdateCurrent()
    {
        if (_previousCurrent != null)
        {
            if (_previousCurrent.State == TypingCharState.Current)
            {
                _previousCurrent.State = TypingCharState.Pending;
            }
        }

        TypingChar? current = CurrentCharacter;

        if (current != null)
        {
            if (current.State == TypingCharState.Pending)
            {
                current.State = TypingCharState.Current;
            }
        }

        _previousCurrent = current;
    }

    public TypingLine? CurrentLine
    {
        get
        {
            if (CurrentLineIndex < 0 ||
                CurrentLineIndex >= Lines.Count)
            {
                return null;
            }

            return Lines[CurrentLineIndex];
        }
    }

    private TypingChar? CurrentCharacter
    {
        get
        {
            TypingLine? line = CurrentLine;

            if (line == null)
            {
                return null;
            }

            if (CurrentCharacterIndex < 0 ||
                CurrentCharacterIndex >= line.Characters.Count)
            {
                return null;
            }

            return line.Characters[CurrentCharacterIndex];
        }
    }


    public bool MoveToNextLine()
    {
        int next = CurrentLineIndex + 1;

        if (next >= Lines.Count)
        {
            return false;
        }

        SetPosition(next, 0);

        return true;
    }

    public bool MoveToNextCharacter()
    {
        TypingLine? line = CurrentLine;

        if (line == null)
        {
            return false;
        }

        int nextChar = CurrentCharacterIndex + 1;

        if (nextChar >= line.Characters.Count)
        {
            return false;
        }

        SetPosition(CurrentLineIndex, nextChar);

        return true;
    }

    public bool CanMoveToPreviousChar()
        => CurrentCharacterIndex > 0;

    public bool MoveToPreviousCharacter()
    {
        if (!CanMoveToPreviousChar())
            return false;

        SetPosition(CurrentLineIndex, CurrentCharacterIndex - 1);
        return true;
    }

    public bool CanMoveToPreviousLine()
        => CurrentLineIndex > 0;

    public bool MoveToPreviousLine()
    {
        if (!CanMoveToPreviousLine())
            return false;

        int prevLineIdx = CurrentLineIndex - 1;
        var prevLine = Lines[prevLineIdx];
        SetPosition(prevLineIdx, prevLine.Characters.Count - 1);

        return true;
    }

    private void ResetCurrentCharacterTo(TypingCharState state)
    {
        TypingChar? c = CurrentCharacter;
        if (c == null)
            return;

        c.State = state;
        c.NbError = 0;
    }


    private bool CanMoveBack()
    {
        return CanMoveToPreviousChar()
            || CanMoveToPreviousLine();
    }
    private bool MoveBack()
    {
        return MoveToPreviousCharacter()
            || MoveToPreviousLine();
    }

    private bool MoveForward()
    {
        if (MoveToNextCharacter())
            return true;


        if (MoveToNextLine())
            return true;

        return false;
    }

    public TypingStatus ProcessInput(char input, Func<char, char> mapper)
    {
        // a key press always means the user is typing again
        Resume();

        if(_isFirstChar)
        {
            _isFirstChar = false;
            _stopwatch.Restart();
            _sessionStopwatch.Restart();
        }

        // BACKSPACE
        if (input == '\b')
        {
            if(! BackReturnEnable)
                return TypingStatus.InProgress;

            if (CanMoveBack())
            {
                ResetCurrentCharacterTo(TypingCharState.Pending);

                if (MoveBack())
                {
                    ResetCurrentCharacterTo(TypingCharState.Current);
                    return TypingStatus.InProgress;
                }
            }

            return TypingStatus.InProgress;
        }

        TypingChar? current = CurrentCharacter;

        if (current == null)
        {
            return TypingStatus.Ended;
        }

        bool success = current.ChallengeValue(mapper(input), _stopwatch.Elapsed);

        HitKeyStatusChanged?.Invoke(success ? HitKeyStatus.Success : HitKeyStatus.Fail);

        if (success || !StopOnError)
        {
            if(MoveForward())
            {
                _stopwatch.Restart();
            }
            else
            {
                _stopwatch.Stop();
                _sessionStopwatch.Stop();
                _isEnded = true;
                return TypingStatus.Ended;
            }
        }

        return TypingStatus.InProgress;
    }

    /// <summary>
    /// Stops the clocks while the user is not typing (e.g. the typing area lost the focus).
    /// </summary>
    public void Pause()
    {
        if (_isFirstChar || _isEnded || _isPaused)
            return;

        _stopwatch.Stop();
        _sessionStopwatch.Stop();
        _isPaused = true;
    }

    /// <summary>
    /// Restarts the clocks after a <see cref="Pause"/>.
    /// </summary>
    public void Resume()
    {
        if (!_isPaused)
            return;

        _stopwatch.Start();
        _sessionStopwatch.Start();
        _isPaused = false;
    }

    /// <summary>
    /// Aggregates the statistics of every typed character, per character.
    /// </summary>
    public TypingSessionResult GetResult()
    {
        Dictionary<char, CharStats> stats = [];

        foreach (TypingChar typed in Lines.SelectMany(l => l.Characters))
        {
            if (!stats.TryGetValue(typed.Character, out CharStats? charStats))
            {
                charStats = new CharStats
                {
                    NbOccurence = 0
                };
                stats[typed.Character] = charStats;
            }

            charStats.NbOccurence++;
            charStats.RespondeTime += typed.RespondeTime;

            if (typed.Errors.Count > 0)
            {
                charStats.NbCharError++;
                charStats.RealErrors.AddRange(typed.Errors);
            }
        }

        return new TypingSessionResult(stats, Duration);
    }
}
