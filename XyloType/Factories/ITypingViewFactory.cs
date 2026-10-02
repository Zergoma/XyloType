using XyloType.Application;
using XyloType.Application.Interfaces;

namespace XyloType.Factories
{
    public interface ITypingViewFactory
    {
        Task<Result<ContentView>> CreateTypingViewAsync(IStringsProvider stringProvider, INavigationService navigationService);
    }
}