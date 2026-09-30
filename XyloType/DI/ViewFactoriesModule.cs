using XyloType.Factories;

namespace XyloType.DI;

public static class ViewFactoriesModule
{
    public static IServiceCollection AddMauiViewFactories(this IServiceCollection services)
    {
        services.AddTransient<ITypingViewFactory, TypingViewFactory>(); 
        services.AddTransient<IStatisticViewFactory, StatisticViewFactory>();

        return services;
    }
}
