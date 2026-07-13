using MediatR;

namespace DriveEase.Lessons.Application.Commands.CompleteLesson;

public sealed record CompleteLessonCommand(Guid LessonId, Guid CallerInstructorId, string? Notes = null) : IRequest;
