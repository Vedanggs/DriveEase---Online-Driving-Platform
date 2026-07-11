using DriveEase.Lessons.Domain.Repositories;
using MediatR;

namespace DriveEase.Lessons.Application.Commands.CancelLesson;

public sealed class CancelLessonHandler(ILessonRepository repository) : IRequestHandler<CancelLessonCommand>
{
    public async Task Handle(CancelLessonCommand request, CancellationToken cancellationToken)
    {
        var lesson = await repository.GetByIdAsync(request.LessonId, cancellationToken)
            ?? throw new InvalidOperationException($"Lesson {request.LessonId} not found.");

        lesson.Cancel();
        await repository.UpdateAsync(lesson, cancellationToken);
    }
}
