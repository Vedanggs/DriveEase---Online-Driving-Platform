using DriveEase.Lessons.Domain.Entities;
using DriveEase.Shared.Outbox;
using Microsoft.EntityFrameworkCore;

namespace DriveEase.Lessons.Infrastructure.Persistence;

public sealed class LessonsDbContext(DbContextOptions<LessonsDbContext> options) : DbContext(options)
{
    public DbSet<Lesson> Lessons => Set<Lesson>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("lessons");

        modelBuilder.Entity<Lesson>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Status).HasConversion<string>().IsRequired();
            e.Property(x => x.Duration).HasConversion(
                v => v.TotalMinutes,
                v => TimeSpan.FromMinutes(v));
            e.HasIndex(x => new { x.StudentId, x.ScheduledAt });
            e.HasIndex(x => x.EnrollmentId);
            e.Ignore(x => x.DomainEvents);
        });

        modelBuilder.Entity<OutboxMessage>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.EventType).IsRequired().HasMaxLength(500);
            e.Property(x => x.Payload).IsRequired();
            e.Property(x => x.CreatedAt).IsRequired();
            e.HasIndex(x => x.ProcessedAt);
        });
    }
}
