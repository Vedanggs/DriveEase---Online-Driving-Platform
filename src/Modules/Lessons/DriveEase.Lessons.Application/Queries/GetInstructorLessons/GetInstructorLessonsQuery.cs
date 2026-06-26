using MediatR;

namespace DriveEase.Lessons.Application.Queries.GetInstructorLessons;

public sealed record InstructorLessonDto(
    Guid Id,
    Guid EnrollmentId,
    Guid StudentId,
    string StudentName,
    DateTime ScheduledAt,
    TimeSpan Duration,
    string Status,
    string? Notes,
    DateTime? CompletedAt);

public sealed record GetInstructorLessonsQuery(Guid InstructorId)
    : IRequest<IReadOnlyList<InstructorLessonDto>>;
