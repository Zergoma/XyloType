namespace XyloType.Application.Models;

/// <param name="Exercises">Exercises added</param>
/// <param name="Words">New words added to the dictionary</param>
/// <param name="Errors">What could not be installed (the rest is kept)</param>
public record StarterPacksSummary(int Exercises, int Words, IReadOnlyList<string> Errors);
