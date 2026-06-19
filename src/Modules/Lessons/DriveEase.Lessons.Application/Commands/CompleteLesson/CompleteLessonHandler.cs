using DriveEase.Lessons.Domain.Repositories;
using MediatR;

namespace DriveEase.Lessons.Application.Commands.CompleteLesson;

public sealed class CompleteLessonHandler(
    ILessonRepository repository) : IRequestHandler<CompleteLessonCommand>
{
    public async Task Handle(CompleteLessonCommand request, CancellationToken cancellationToken)
    {
        var lesson = await repository.GetByIdAsync(request.LessonId, cancellationToken)
            ?? throw new InvalidOperationException($"Lesson {request.LessonId} not found.");

        lesson.Complete(request.Notes);
        await repository.UpdateAsync(lesson, cancellationToken);
        // OutboxInterceptor captures LessonCompletedEvent atomically during UpdateAsync
    }
}
