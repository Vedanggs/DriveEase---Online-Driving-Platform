using DriveEase.Shared.Domain;

namespace DriveEase.Lessons.Domain.Events;

public sealed record LessonPackageCompletedEvent(
    Guid EventId,
    DateTime OccurredAt,
    Guid EnrollmentId,
    Guid StudentId) : IDomainEvent, IIntegrationEvent
{
    public string EventType => nameof(LessonPackageCompletedEvent);

    public static LessonPackageCompletedEvent Create(Guid enrollmentId, Guid studentId) =>
        new(Guid.NewGuid(), DateTime.UtcNow, enrollmentId, studentId);
}
