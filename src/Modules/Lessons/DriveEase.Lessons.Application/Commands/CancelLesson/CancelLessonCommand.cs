using MediatR;

namespace DriveEase.Lessons.Application.Commands.CancelLesson;

public sealed record CancelLessonCommand(Guid LessonId) : IRequest;
