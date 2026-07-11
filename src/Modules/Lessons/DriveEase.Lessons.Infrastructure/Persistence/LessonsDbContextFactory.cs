using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace DriveEase.Lessons.Infrastructure.Persistence;

public sealed class LessonsDbContextFactory : IDesignTimeDbContextFactory<LessonsDbContext>
{
    public LessonsDbContext CreateDbContext(string[] args)
    {
        var opts = new DbContextOptionsBuilder<LessonsDbContext>()
            .UseSqlServer("Server=localhost\\SQLEXPRESS;Database=DriveEase;Trusted_Connection=True;TrustServerCertificate=True;")
            .Options;
        return new LessonsDbContext(opts);
    }
}
