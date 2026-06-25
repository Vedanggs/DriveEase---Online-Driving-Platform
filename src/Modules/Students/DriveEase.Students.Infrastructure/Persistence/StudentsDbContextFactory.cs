using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace DriveEase.Students.Infrastructure.Persistence;

public sealed class StudentsDbContextFactory : IDesignTimeDbContextFactory<StudentsDbContext>
{
    public StudentsDbContext CreateDbContext(string[] args)
    {
        var opts = new DbContextOptionsBuilder<StudentsDbContext>()
            .UseSqlite("Data Source=driveease-students.db")
            .Options;
        return new StudentsDbContext(opts);
    }
}
