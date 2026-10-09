using Microsoft.EntityFrameworkCore;

using XyloType.Application.Interfaces;
using XyloType.Domain.Entities;
using XyloType.Infrastructure.DbContexts;

namespace XyloType.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly IDbContextFactory<DactyloDbContext> _factory;

    public UserRepository(IDbContextFactory<DactyloDbContext> factory)
    {
        _factory = factory;
    }

    public async Task<List<UserProfile>> GetAllAsync()
    {
        await using var ctx = await _factory.CreateDbContextAsync();

        List<UserProfile> users = await ctx.Users
            .AsNoTracking()
            .ToListAsync();

        // sorted here: the culture of the user, not the collation of SQLite
        return [.. users.OrderBy(u => u.Name, StringComparer.CurrentCultureIgnoreCase)];
    }

    public async Task<UserProfile> AddAsync(string name, DateTime createdAtUtc)
    {
        await using var ctx = await _factory.CreateDbContextAsync();

        UserProfile user = new() { Name = name, CreatedAtUtc = createdAtUtc };
        ctx.Users.Add(user);
        await ctx.SaveChangesAsync();

        return user;
    }

    public async Task RenameAsync(int userId, string name)
    {
        await using var ctx = await _factory.CreateDbContextAsync();

        await ctx.Users
            .Where(u => u.Id == userId)
            .ExecuteUpdateAsync(s => s.SetProperty(u => u.Name, name));
    }

    public async Task DeleteAsync(int userId)
    {
        await using var ctx = await _factory.CreateDbContextAsync();

        // the attempts first: a bulk delete does not cascade
        await ctx.ExerciseAttempts
            .Where(a => a.UserId == userId)
            .ExecuteDeleteAsync();

        await ctx.Users
            .Where(u => u.Id == userId)
            .ExecuteDeleteAsync();
    }
}
