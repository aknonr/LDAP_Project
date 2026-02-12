using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Infrastructure.Persistence;

/// <summary>
/// EF Core tooling icin design-time DbContext fabrikasi.
/// </summary>
public sealed class AdpmDbContextFactory : IDesignTimeDbContextFactory<AdpmDbContext>
{
    public AdpmDbContext CreateDbContext(string[] args)
    {
        // Tooling icin guvenli bir varsayilan connection string kullanir.
        var connectionString = Environment.GetEnvironmentVariable("ADPM_CONNECTIONSTRING")
            ?? "Server=(localdb)\\mssqllocaldb;Database=AdpmDesignTime;Trusted_Connection=True;TrustServerCertificate=True;";

        var optionsBuilder = new DbContextOptionsBuilder<AdpmDbContext>();
        optionsBuilder.UseSqlServer(connectionString);

        return new AdpmDbContext(optionsBuilder.Options);
    }
}
