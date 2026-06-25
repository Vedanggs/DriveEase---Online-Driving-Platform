using DriveEase.Lessons.Domain.Events;
using DriveEase.Shared.Domain;

namespace DriveEase.Lessons.Domain.Entities;

public enum LessonStatus { Scheduled, Completed, Cancelled }

public sealed class Lesson : AggregateRoot<Guid>
{
    public Guid EnrollmentId { get; private set; }
    public Guid StudentId { get; private set; }
    public string StudentName { get; private set; } = string.Empty;
    public Guid InstructorId { get; private set; }
    public DateTime ScheduledAt { get; private set; }
    public TimeSpan Duration { get; private set; }
    public LessonStatus Status { get; private set; }
    public string? Notes { get; private set; }
    public DateTime? CompletedAt { get; private set; }

    private Lesson() { }

    public static Lesson Book(Guid enrollmentId, Guid studentId, string studentName, Guid instructorId, DateTime scheduledAt, TimeSpan duration)
    {
        if (scheduledAt <= DateTime.UtcNow)
            throw new InvalidOperationException("Lesson must be scheduled in the future.");

        var lesson = new Lesson
        {
            Id = Guid.NewGuid(),
            EnrollmentId = enrollmentId,
            StudentId = studentId,
            StudentName = studentName,
            InstructorId = instructorId,
            ScheduledAt = scheduledAt,
            Duration = duration,
            Status = LessonStatus.Scheduled
        };

        lesson.RaiseDomainEvent(LessonBookedEvent.Create(lesson.Id, studentId, studentName, instructorId, scheduledAt));
        return lesson;
    }

    public void Complete(string? notes = null)
    {
        if (Status != LessonStatus.Scheduled)
            throw new InvalidOperationException($"Cannot complete a lesson in status '{Status}'.");

        Status = LessonStatus.Completed;
        Notes = notes;
        CompletedAt = DateTime.UtcNow;

        RaiseDomainEvent(LessonCompletedEvent.Create(Id, EnrollmentId, StudentId, InstructorId));
    }

    public void Cancel()
    {
        if (Status == LessonStatus.Completed)
            throw new InvalidOperationException("Cannot cancel a completed lesson.");

        Status = LessonStatus.Cancelled;
    }
}
