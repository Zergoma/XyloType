using Microsoft.EntityFrameworkCore;

using XyloType.Application.Interfaces;
using XyloType.Domain.Entities;
using XyloType.Infrastructure.DbContexts;

namespace XyloType.Infrastructure.Repositories;

public class ImportedSourceRepository : IImportedSourceRepository
{
    private readonly IDbContextFactory<DactyloDbContext> _factory;

    public ImportedSourceRepository(IDbContextFactory<DactyloDbContext> factory)
    {
        _factory = factory;
    }

    public async Task<List<ImportedSource>> GetAllAsync()
    {
        await using var ctx = await _factory.CreateDbContextAsync();

        // SQLite cannot order by DateTime offsets: the id follows the import order
        return await ctx.ImportedSources
            .OrderByDescending(s => s.Id)
            .ToListAsync();
    }
}
