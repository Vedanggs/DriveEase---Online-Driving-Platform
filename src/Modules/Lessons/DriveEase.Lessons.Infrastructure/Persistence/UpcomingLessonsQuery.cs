using DriveEase.Lessons.Domain.Entities;
using DriveEase.Lessons.Infrastructure.Workers;
using Microsoft.EntityFrameworkCore;

namespace DriveEase.Lessons.Infrastructure.Persistence;

public sealed class UpcomingLessonsQuery(LessonsDbContext dbContext) : IUpcomingLessonsQuery
{
    public async Task<IReadOnlyList<UpcomingLesson>> GetScheduledWithin25HoursAsync(
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var cutoff = now.AddHours(25);

        return await dbContext.Lessons
            .Where(l => l.Status == LessonStatus.Scheduled
                     && l.ScheduledAt >= now
                     && l.ScheduledAt <= cutoff)
            .Select(l => new UpcomingLesson(l.Id, l.StudentId, l.StudentName, l.InstructorId, l.ScheduledAt))
            .ToListAsync(cancellationToken);
    }
}
