namespace XyloType.Application.Models;

/// <summary>
/// Sound played on a correct key.
/// </summary>
public enum OkSoundMode
{
    /// <summary>
    /// A random note among a few.
    /// </summary>
    Standard = 0,

    /// <summary>
    /// The next note of an instrumental piece.
    /// </summary>
    Instrumental,

    /// <summary>
    /// The next note of a song.
    /// </summary>
    Song
}
