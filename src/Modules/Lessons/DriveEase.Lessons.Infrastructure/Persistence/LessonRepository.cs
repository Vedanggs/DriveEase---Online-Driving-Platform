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
}
