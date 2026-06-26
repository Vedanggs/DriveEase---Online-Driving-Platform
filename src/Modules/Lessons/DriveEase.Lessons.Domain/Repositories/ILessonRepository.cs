using DriveEase.Lessons.Domain.Entities;

namespace DriveEase.Lessons.Domain.Repositories;

public interface ILessonRepository
{
    Task<Lesson?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Lesson>> GetAllByStudentAsync(Guid studentId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Lesson>> GetUpcomingByStudentAsync(Guid studentId, TimeSpan within, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Lesson>> GetByEnrollmentAsync(Guid enrollmentId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Lesson>> GetByInstructorAsync(Guid instructorId, CancellationToken cancellationToken = default);
    Task<int> CountByEnrollmentAsync(Guid enrollmentId, CancellationToken cancellationToken = default);
    Task<int> CountCompletedByEnrollmentAsync(Guid enrollmentId, CancellationToken cancellationToken = default);
    Task<bool> HasConflictAsync(Guid instructorId, DateTime scheduledAt, TimeSpan duration, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Lesson>> GetBookedSlotsAsync(Guid instructorId, DateTime dateUtc, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Lesson>> GetExpiredScheduledAsync(DateTime cutoffUtc, CancellationToken cancellationToken = default);
    Task AddAsync(Lesson lesson, CancellationToken cancellationToken = default);
    Task UpdateAsync(Lesson lesson, CancellationToken cancellationToken = default);
    Task UpdateBatchAsync(IEnumerable<Lesson> lessons, CancellationToken cancellationToken = default);
}
