using DriveEase.Shared.Domain;

namespace DriveEase.Lessons.Domain.Events;

public sealed record LessonBookedEvent(
    Guid EventId,
    DateTime OccurredAt,
    Guid LessonId,
    Guid StudentId,
    string StudentName,
    Guid InstructorId,
    DateTime ScheduledAt) : IDomainEvent, IIntegrationEvent
{
    public string EventType => nameof(LessonBookedEvent);

    public static LessonBookedEvent Create(Guid lessonId, Guid studentId, string studentName, Guid instructorId, DateTime scheduledAt) =>
        new(Guid.NewGuid(), DateTime.UtcNow, lessonId, studentId, studentName, instructorId, scheduledAt);
}
