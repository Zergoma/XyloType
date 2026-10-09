using XyloType.Application.DTOs;
using XyloType.Application.Models;

namespace XyloType.Application.Interfaces;

/// <summary>
/// Offers and installs, in one go, the packs that make the app ready at once (first start).
/// </summary>
public interface IStarterPacksOrchestrator
{
    /// <summary>
    /// What is missing for the keyboard (empty without connection: nothing to offer).
    /// </summary>
    Task<StarterPacksOffer> GetOfferAsync(KeyBoardLayoutDto keyboard, CancellationToken cancellationToken = default);

    /// <summary>
    /// Downloads and imports the packs of the offer: the exercises first (a few seconds), then the words.
    /// </summary>
    /// <param name="progress">What is being done, to show to the user</param>
    Task<StarterPacksSummary> InstallAsync(
        StarterPacksOffer offer,
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default);
}
