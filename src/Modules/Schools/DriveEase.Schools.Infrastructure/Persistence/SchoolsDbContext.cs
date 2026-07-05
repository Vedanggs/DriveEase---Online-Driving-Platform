using DriveEase.Schools.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DriveEase.Schools.Infrastructure.Persistence;

public sealed class SchoolsDbContext(DbContextOptions<SchoolsDbContext> options) : DbContext(options)
{
    public DbSet<DrivingSchool> Schools => Set<DrivingSchool>();
    public DbSet<Instructor> Instructors => Set<Instructor>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("schools");

        modelBuilder.Entity<DrivingSchool>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.Address).HasMaxLength(500).IsRequired();
            e.Property(x => x.ContactEmail).HasMaxLength(200).IsRequired();
            e.Ignore(x => x.DomainEvents);
        });

        modelBuilder.Entity<Instructor>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.FullName).HasMaxLength(200).IsRequired();
            e.Property(x => x.LicenseNumber).HasMaxLength(50).IsRequired();
            e.Property(x => x.Email).HasMaxLength(200).IsRequired(false);
            e.Property(x => x.PasswordHash).HasMaxLength(200).IsRequired(false);
            e.HasIndex(x => x.SchoolId);
        });
    }
}
