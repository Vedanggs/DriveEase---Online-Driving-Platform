using MediatR;

namespace DriveEase.Lessons.Application.Queries.GetStudentLessons;

public sealed record StudentLessonDto(
    Guid Id,
    Guid EnrollmentId,
    DateTime ScheduledAt,
    TimeSpan Duration,
    string Status,
    string? Notes,
    DateTime? CompletedAt);

public sealed record GetStudentLessonsQuery(Guid StudentId) : IRequest<IReadOnlyList<StudentLessonDto>>;
