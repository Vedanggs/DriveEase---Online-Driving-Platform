using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace DriveEase.Enrollments.Infrastructure.Persistence;

public sealed class EnrollmentsDbContextFactory : IDesignTimeDbContextFactory<EnrollmentsDbContext>
{
    public EnrollmentsDbContext CreateDbContext(string[] args)
    {
        var opts = new DbContextOptionsBuilder<EnrollmentsDbContext>()
            .UseSqlServer("Server=localhost\\SQLEXPRESS;Database=DriveEase;Trusted_Connection=True;TrustServerCertificate=True;")
            .Options;
        return new EnrollmentsDbContext(opts);
    }
}
