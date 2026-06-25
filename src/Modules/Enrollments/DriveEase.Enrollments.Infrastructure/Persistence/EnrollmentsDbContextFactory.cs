using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace DriveEase.Enrollments.Infrastructure.Persistence;

public sealed class EnrollmentsDbContextFactory : IDesignTimeDbContextFactory<EnrollmentsDbContext>
{
    public EnrollmentsDbContext CreateDbContext(string[] args)
    {
        var opts = new DbContextOptionsBuilder<EnrollmentsDbContext>()
            .UseSqlite("Data Source=driveease-enrollments.db")
            .Options;
        return new EnrollmentsDbContext(opts);
    }
}
