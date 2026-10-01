using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using XyloType.Infrastructure.DbContexts;

namespace XyloType.Infrastructure.DI;

public static class InfrastructureDbInitModule
{
    /// <summary>
    /// Creates the database or upgrades it to the latest migration.
    /// </summary>
    public static void InitUpgradeInfrastructure(this IServiceProvider services)
    {
        ILogger logger =
            services.GetRequiredService<ILoggerFactory>()
                .CreateLogger(nameof(InfrastructureDbInitModule));

        try
        {
            IDbContextFactory<DactyloDbContext> factory =
                services.GetRequiredService<IDbContextFactory<DactyloDbContext>>();

            using DactyloDbContext dbContext = factory.CreateDbContext();

            logger.LogInformation(
                "Database {DataSource}: applying migrations",
                dbContext.Database.GetDbConnection().DataSource);

            dbContext.Database.Migrate();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Database initialization failed");
        }
    }
}
