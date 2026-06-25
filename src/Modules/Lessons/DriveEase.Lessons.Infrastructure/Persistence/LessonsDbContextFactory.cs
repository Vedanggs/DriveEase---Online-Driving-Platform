using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace DriveEase.Lessons.Infrastructure.Persistence;

public sealed class LessonsDbContextFactory : IDesignTimeDbContextFactory<LessonsDbContext>
{
    public LessonsDbContext CreateDbContext(string[] args)
    {
        var opts = new DbContextOptionsBuilder<LessonsDbContext>()
            .UseSqlite("Data Source=driveease-lessons.db")
            .Options;
        return new LessonsDbContext(opts);
    }
}
