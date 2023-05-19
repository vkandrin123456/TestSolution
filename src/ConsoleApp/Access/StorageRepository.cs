using ConsoleApp.Models;
using Microsoft.EntityFrameworkCore;

namespace ConsoleApp.Access;
public class StorageRepository : IStorageRepository
{
    private readonly IDbContextFactory<StorageContext> _contextFactory;
    public StorageRepository(IDbContextFactory<StorageContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<Source> AddAsync(Source entity, CancellationToken cancellation = default)
    {
        using var ctx = await _contextFactory.CreateDbContextAsync(cancellation);
        ctx.Set<Source>().Add(entity);
        await ctx.SaveChangesAsync(cancellation);
        return entity;
    }

    public async Task<ICollection<Source>> GetAllAsync(CancellationToken cancellation = default)
    {
        using var ctx = await _contextFactory.CreateDbContextAsync(cancellation);
        return await ctx.Set<Source>().ToListAsync(cancellation);
    }
}
