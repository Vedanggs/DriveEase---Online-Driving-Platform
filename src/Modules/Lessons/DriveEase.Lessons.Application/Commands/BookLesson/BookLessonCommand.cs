using MediatR;

namespace DriveEase.Lessons.Application.Commands.BookLesson;

public sealed record BookLessonCommand(
    Guid EnrollmentId,
    Guid StudentId,
    string StudentName,
    Guid InstructorId,
    string InstructorName,
    DateTime ScheduledAt,
    TimeSpan Duration) : IRequest<Guid>;
