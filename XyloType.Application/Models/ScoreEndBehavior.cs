namespace XyloType.Application.Models;

/// <summary>
/// What happens when the piece of music played on correct keys is over.
/// </summary>
public enum ScoreEndBehavior
{
    /// <summary>
    /// The same piece starts again.
    /// </summary>
    Loop = 0,

    /// <summary>
    /// Another enabled piece is picked at random.
    /// </summary>
    NextScore
}
