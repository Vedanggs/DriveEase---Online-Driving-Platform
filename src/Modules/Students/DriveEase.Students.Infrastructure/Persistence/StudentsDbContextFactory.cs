using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace DriveEase.Students.Infrastructure.Persistence;

public sealed class StudentsDbContextFactory : IDesignTimeDbContextFactory<StudentsDbContext>
{
    public StudentsDbContext CreateDbContext(string[] args)
    {
        var opts = new DbContextOptionsBuilder<StudentsDbContext>()
            .UseSqlServer("Server=localhost\\SQLEXPRESS;Database=DriveEase;Trusted_Connection=True;TrustServerCertificate=True;")
            .Options;
        return new StudentsDbContext(opts);
    }
}