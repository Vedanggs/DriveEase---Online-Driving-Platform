using DriveEase.Notifications.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DriveEase.Notifications.Infrastructure.Persistence;

public sealed class NotificationsDbContext(DbContextOptions<NotificationsDbContext> options) : DbContext(options)
{
    public DbSet<InstructorNotification> InstructorNotifications => Set<InstructorNotification>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("notifications");

        modelBuilder.Entity<InstructorNotification>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Type).HasMaxLength(50).IsRequired();
            e.Property(x => x.StudentName).HasMaxLength(200).IsRequired();
            e.Property(x => x.Detail).HasMaxLength(500).IsRequired();
            e.HasIndex(x => x.InstructorId);
        });
    }
}
