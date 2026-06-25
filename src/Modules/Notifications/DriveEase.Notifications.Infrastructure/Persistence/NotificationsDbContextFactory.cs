using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace DriveEase.Notifications.Infrastructure.Persistence;

public sealed class NotificationsDbContextFactory : IDesignTimeDbContextFactory<NotificationsDbContext>
{
    public NotificationsDbContext CreateDbContext(string[] args)
    {
        var opts = new DbContextOptionsBuilder<NotificationsDbContext>()
            .UseSqlite("Data Source=driveease-notifications.db")
            .Options;
        return new NotificationsDbContext(opts);
    }
}
