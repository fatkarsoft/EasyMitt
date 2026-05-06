using EasyMitt.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace EasyMitt.Infrastructure.Persistence;

public sealed class EasyMittDbContext(DbContextOptions<EasyMittDbContext> options) : DbContext(options)
{
    public DbSet<InvoiceDraftEntity> InvoiceDrafts => Set<InvoiceDraftEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(EasyMittDbContext).Assembly);
    }
}
