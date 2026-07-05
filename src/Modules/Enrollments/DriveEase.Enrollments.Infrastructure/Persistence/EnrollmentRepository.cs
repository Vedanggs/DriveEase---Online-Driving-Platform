using DriveEase.Enrollments.Domain.Aggregates;
using DriveEase.Enrollments.Domain.Repositories;
using DriveEase.Enrollments.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace DriveEase.Enrollments.Infrastructure.Persistence;

public sealed class EnrollmentRepository(EnrollmentsDbContext dbContext) : IEnrollmentRepository
{
    public Task<Enrollment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.Enrollments.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

    public Task<Enrollment?> GetActiveByStudentIdAsync(Guid studentId, CancellationToken cancellationToken = default) =>
        dbContext.Enrollments.AsNoTracking().FirstOrDefaultAsync(
            e => e.StudentId == studentId && e.Status == EnrollmentStatus.Active,
            cancellationToken);

    public async Task<IReadOnlyList<Enrollment>> GetPendingPaymentOlderThanAsync(
        TimeSpan age, CancellationToken cancellationToken = default)
    {
        var cutoff = DateTime.UtcNow - age;
        return await dbContext.Enrollments
            .AsNoTracking()
            .Where(e => e.PaymentStatus == PaymentStatus.Pending
                     && e.Status == EnrollmentStatus.Pending
                     && e.EnrolledAt < cutoff)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Enrollment enrollment, CancellationToken cancellationToken = default)
    {
        await dbContext.Enrollments.AddAsync(enrollment, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Enrollment enrollment, CancellationToken cancellationToken = default)
    {
        dbContext.Enrollments.Update(enrollment);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
