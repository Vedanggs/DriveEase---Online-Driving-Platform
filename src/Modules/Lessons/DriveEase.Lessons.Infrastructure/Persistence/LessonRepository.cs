using DriveEase.Lessons.Domain.Entities;
using DriveEase.Lessons.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace DriveEase.Lessons.Infrastructure.Persistence;

public sealed class LessonRepository(LessonsDbContext dbContext) : ILessonRepository
{
    public Task<Lesson?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.Lessons
            .AsNoTracking()
            .FirstOrDefaultAsync(l => l.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Lesson>> GetAllByStudentAsync(
        Guid studentId, CancellationToken cancellationToken = default) =>
        await dbContext.Lessons
            .AsNoTracking()
            .Where(l => l.StudentId == studentId)
            .OrderByDescending(l => l.ScheduledAt)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Lesson>> GetUpcomingByStudentAsync(
        Guid studentId, TimeSpan within, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var cutoff = now.Add(within);
        return await dbContext.Lessons
            .AsNoTracking()
            .Where(l => l.StudentId == studentId
                     && l.Status == LessonStatus.Scheduled
                     && l.ScheduledAt >= now
                     && l.ScheduledAt <= cutoff)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Lesson>> GetByEnrollmentAsync(
        Guid enrollmentId, CancellationToken cancellationToken = default) =>
        await dbContext.Lessons
            .AsNoTracking()
            .Where(l => l.EnrollmentId == enrollmentId)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Lesson>> GetByInstructorAsync(
        Guid instructorId, CancellationToken cancellationToken = default) =>
        await dbContext.Lessons
            .AsNoTracking()
            .Where(l => l.InstructorId == instructorId && l.Status != LessonStatus.Cancelled)
            .OrderBy(l => l.ScheduledAt)
            .ToListAsync(cancellationToken);

    public Task<int> CountByEnrollmentAsync(Guid enrollmentId, CancellationToken cancellationToken = default) =>
        dbContext.Lessons
            .CountAsync(l => l.EnrollmentId == enrollmentId && l.Status != LessonStatus.Cancelled, cancellationToken);

    public async Task<bool> HasConflictAsync(Guid instructorId, DateTime scheduledAt, TimeSpan duration,
        CancellationToken cancellationToken = default)
    {
        // Load candidate lessons in a narrow window (same day ± 2 hrs) and check overlap in memory.
        // EF/SQLite can't translate TimeSpan arithmetic, so we avoid it in LINQ.
        var windowStart = scheduledAt.AddHours(-2);
        var windowEnd   = scheduledAt.Add(duration).AddHours(2);

        var candidates = await dbContext.Lessons
            .AsNoTracking()
            .Where(l => l.InstructorId == instructorId
                     && l.Status != LessonStatus.Cancelled
                     && l.ScheduledAt >= windowStart
                     && l.ScheduledAt < windowEnd)
            .ToListAsync(cancellationToken);

        var newEnd = scheduledAt.Add(duration);
        return candidates.Any(l =>
            l.ScheduledAt < newEnd &&
            l.ScheduledAt.Add(l.Duration) > scheduledAt);
    }

    public async Task<IReadOnlyList<Lesson>> GetBookedSlotsAsync(Guid instructorId, DateTime dateUtc,
        CancellationToken cancellationToken = default)
    {
        var dayStart = dateUtc.Date;
        var dayEnd   = dayStart.AddDays(1);
        return await dbContext.Lessons
            .AsNoTracking()
            .Where(l => l.InstructorId == instructorId
                     && l.Status != LessonStatus.Cancelled
                     && l.ScheduledAt >= dayStart
                     && l.ScheduledAt < dayEnd)
            .ToListAsync(cancellationToken);
    }

    public Task<int> CountCompletedByEnrollmentAsync(Guid enrollmentId, CancellationToken cancellationToken = default) =>
        dbContext.Lessons
            .CountAsync(l => l.EnrollmentId == enrollmentId && l.Status == LessonStatus.Completed, cancellationToken);

    // Returns all Scheduled lessons that have already started (ScheduledAt in the past).
    // Duration can't be used in EF/SQLite LINQ, so the end-time check is done in memory
    // by the caller after this query returns.
    public async Task<IReadOnlyList<Lesson>> GetExpiredScheduledAsync(
        DateTime nowUtc, CancellationToken cancellationToken = default) =>
        await dbContext.Lessons
            .Where(l => l.Status == LessonStatus.Scheduled && l.ScheduledAt < nowUtc)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(Lesson lesson, CancellationToken cancellationToken = default)
    {
        await dbContext.Lessons.AddAsync(lesson, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Lesson lesson, CancellationToken cancellationToken = default)
    {
        dbContext.Lessons.Update(lesson);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateBatchAsync(IEnumerable<Lesson> lessons, CancellationToken cancellationToken = default)
    {
        foreach (var lesson in lessons)
            dbContext.Lessons.Update(lesson);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
