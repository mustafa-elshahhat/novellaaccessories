using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Novella.Infrastructure.Persistence;

/// <summary>
/// Design-time factory so EF Core tooling (migrations) can build the context without running the
/// API host. The connection string here is only used for scaffolding metadata; it is read from the
/// environment when available so commands can target a real database.
/// </summary>
public sealed class NovellaDbContextFactory : IDesignTimeDbContextFactory<NovellaDbContext>
{
    public NovellaDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? "Server=localhost;Database=NovellaAccessories;Trusted_Connection=True;TrustServerCertificate=True";

        var options = new DbContextOptionsBuilder<NovellaDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        return new NovellaDbContext(options);
    }
}
