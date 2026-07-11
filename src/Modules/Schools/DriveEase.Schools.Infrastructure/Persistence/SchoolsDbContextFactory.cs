using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace DriveEase.Schools.Infrastructure.Persistence;

public sealed class SchoolsDbContextFactory : IDesignTimeDbContextFactory<SchoolsDbContext>
{
    public SchoolsDbContext CreateDbContext(string[] args)
    {
        var opts = new DbContextOptionsBuilder<SchoolsDbContext>()
            .UseSqlServer("Server=localhost\\SQLEXPRESS;Database=DriveEase;Trusted_Connection=True;TrustServerCertificate=True;")
            .Options;
        return new SchoolsDbContext(opts);
    }
}
