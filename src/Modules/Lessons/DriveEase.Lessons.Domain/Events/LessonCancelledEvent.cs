using DriveEase.Shared.Domain;

namespace DriveEase.Lessons.Domain.Events;

public sealed record LessonCancelledEvent(
    Guid EventId,
    DateTime OccurredAt,
    Guid LessonId,
    Guid StudentId,
    string StudentName,
    Guid InstructorId,
    DateTime ScheduledAt) : IDomainEvent, IIntegrationEvent
{
    public string EventType => nameof(LessonCancelledEvent);

    public static LessonCancelledEvent Create(Guid lessonId, Guid studentId, string studentName, Guid instructorId, DateTime scheduledAt) =>
        new(Guid.NewGuid(), DateTime.UtcNow, lessonId, studentId, studentName, instructorId, scheduledAt);
}
