using XyloType.Application;
using XyloType.Application.Interfaces;
using XyloType.Domain.Typing.Analysis;

namespace XyloType.Factories
{
    public interface IStatisticViewFactory
    {
        Task<Result<ContentPage>> Create(
            TypingSessionResult result,
            INavigationService navigationService);
    }
}
