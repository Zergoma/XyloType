namespace XyloType.Application.Models;

/// <summary>
/// Instrument playing the notes (correct key notes and error sound).
/// </summary>
public enum InstrumentChoice
{
    /// <summary>
    /// Synthesized xylophone.
    /// </summary>
    Xylophone = 0,

    Piano,

    /// <summary>
    /// Recorded xylophone.
    /// </summary>
    XylophoneRecorded,

    Marimba,
    Vibraphone,
    Glockenspiel,

    /// <summary>
    /// One of the enabled instruments, picked at each exercise.
    /// </summary>
    Random
}
