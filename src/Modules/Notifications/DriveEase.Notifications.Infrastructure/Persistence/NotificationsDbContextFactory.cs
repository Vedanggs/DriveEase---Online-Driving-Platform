using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace DriveEase.Notifications.Infrastructure.Persistence;

public sealed class NotificationsDbContextFactory : IDesignTimeDbContextFactory<NotificationsDbContext>
{
    public NotificationsDbContext CreateDbContext(string[] args)
    {
        var opts = new DbContextOptionsBuilder<NotificationsDbContext>()
            .UseSqlServer("Server=localhost\\SQLEXPRESS;Database=DriveEase;Trusted_Connection=True;TrustServerCertificate=True;")
            .Options;
        return new NotificationsDbContext(opts);
    }
}
