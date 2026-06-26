using MediatR;

namespace DriveEase.Lessons.Application.Queries.GetStudentLessons;

public sealed record StudentLessonDto(
    Guid Id,
    Guid EnrollmentId,
    Guid InstructorId,
    DateTime ScheduledAt,
    TimeSpan Duration,
    string Status,
    string? Notes,
    DateTime? CompletedAt,
    string InstructorName);

public sealed record GetStudentLessonsQuery(Guid StudentId) : IRequest<IReadOnlyList<StudentLessonDto>>;
