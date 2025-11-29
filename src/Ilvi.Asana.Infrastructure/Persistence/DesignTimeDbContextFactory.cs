using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Ilvi.Asana.Infrastructure.Persistence;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AsanaDbContext>
{
    public AsanaDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AsanaDbContext>();
        optionsBuilder.UseSqlServer("Server=localhost,1433;Database=AsanaSync;User Id=sa;Password=MyPass123!;TrustServerCertificate=True;");

        return new AsanaDbContext(optionsBuilder.Options);
    }
}