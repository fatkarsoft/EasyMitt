using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace EasyMitt.Infrastructure.Persistence;

public sealed class EasyMittDbContextFactory : IDesignTimeDbContextFactory<EasyMittDbContext>
{
    public EasyMittDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<EasyMittDbContext>();
        var cs = Environment.GetEnvironmentVariable("EASYMITT_PG")
            ?? "Host=localhost;Port=5432;Database=easymitt;Username=postgres;Password=postgres";
        optionsBuilder.UseNpgsql(cs);
        return new EasyMittDbContext(optionsBuilder.Options);
    }
}
