using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace DriveEase.Schools.Infrastructure.Persistence;

public sealed class SchoolsDbContextFactory : IDesignTimeDbContextFactory<SchoolsDbContext>
{
    public SchoolsDbContext CreateDbContext(string[] args)
    {
        var opts = new DbContextOptionsBuilder<SchoolsDbContext>()
            .UseSqlite("Data Source=driveease-schools.db")
            .Options;
        return new SchoolsDbContext(opts);
    }
}
