namespace ConsoleApp.Access;

using ConsoleApp.Models;
using Microsoft.EntityFrameworkCore;


public class StorageContext : DbContext 
{
    public DbSet<Source> Sources { get; }

    public StorageContext(DbContextOptions<StorageContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder model)
    {
        model.Entity<Source>(
            entity =>
            {
                entity.ToTable($"{nameof(Source)}s");
                entity.HasKey(e => e.Url);
            });
    }
}
