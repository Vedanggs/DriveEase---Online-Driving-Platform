using DriveEase.Enrollments.Domain.Aggregates;
using DriveEase.Shared.Outbox;
using Microsoft.EntityFrameworkCore;

namespace DriveEase.Enrollments.Infrastructure.Persistence;

public sealed class EnrollmentsDbContext(DbContextOptions<EnrollmentsDbContext> options) : DbContext(options)
{
    public DbSet<Enrollment> Enrollments => Set<Enrollment>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("enrollments");

        modelBuilder.Entity<Enrollment>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.StudentId).IsRequired();
            e.Property(x => x.DrivingSchoolId).IsRequired();
            e.Property(x => x.InstructorId);
            e.Property(x => x.Fee).HasColumnType("decimal(18,2)").IsRequired();
            e.Property(x => x.PaymentStatus).HasConversion<string>().IsRequired();
            e.Property(x => x.Status).HasConversion<string>().IsRequired();
            e.Property(x => x.EnrolledAt).IsRequired();
            e.Property(x => x.PaymentConfirmedAt);
            e.Property(x => x.CancelledAt);

            e.Ignore(x => x.DomainEvents);

            e.HasIndex(x => x.StudentId);
            e.HasIndex(x => new { x.StudentId, x.Status });
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
