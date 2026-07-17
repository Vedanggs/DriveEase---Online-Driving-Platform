using DriveEase.Lessons.Domain.Repositories;
using MediatR;

namespace DriveEase.Lessons.Application.Commands.CancelLesson;

public sealed class CancelLessonHandler(ILessonRepository repository) : IRequestHandler<CancelLessonCommand>
{
    public async Task Handle(CancelLessonCommand request, CancellationToken cancellationToken)
    {
        var lesson = await repository.GetByIdAsync(request.LessonId, cancellationToken)
            ?? throw new InvalidOperationException($"Lesson {request.LessonId} not found.");

        // Student can't cancel within 15 minutes of the start time.
        // ScheduledAt is stored in UTC, so compare against UtcNow.
        if (lesson.ScheduledAt - DateTime.UtcNow < TimeSpan.FromMinutes(15))
            throw new InvalidOperationException(
                "Lessons can't be cancelled within 15 minutes of the start time.");

        lesson.Cancel();
        await repository.UpdateAsync(lesson, cancellationToken);
    }
}
