using DriveEase.Enrollments.Domain.Repositories;
using DriveEase.Lessons.Domain.Repositories;
using MediatR;

namespace DriveEase.Lessons.Application.Commands.CompleteLesson;

public sealed class CompleteLessonHandler(
    ILessonRepository repository,
    IEnrollmentRepository enrollmentRepository) : IRequestHandler<CompleteLessonCommand>
{
    private const int MaxLessonsPerEnrollment = 5;

    public async Task Handle(CompleteLessonCommand request, CancellationToken cancellationToken)
    {
        var lesson = await repository.GetByIdAsync(request.LessonId, cancellationToken)
            ?? throw new InvalidOperationException($"Lesson {request.LessonId} not found.");

        var completedCount = await repository.CountCompletedByEnrollmentAsync(lesson.EnrollmentId, cancellationToken);
        var isLastInPackage = completedCount + 1 >= MaxLessonsPerEnrollment;

        lesson.Complete(request.Notes, isLastInPackage);
        await repository.UpdateAsync(lesson, cancellationToken);

        if (isLastInPackage)
            await TryCompleteEnrollmentAsync(lesson.EnrollmentId, cancellationToken);
    }

    private async Task TryCompleteEnrollmentAsync(Guid enrollmentId, CancellationToken cancellationToken)
    {
        var enrollment = await enrollmentRepository.GetByIdAsync(enrollmentId, cancellationToken);
        if (enrollment is null) return;

        try
        {
            enrollment.Complete();
            await enrollmentRepository.UpdateAsync(enrollment, cancellationToken);
        }
        catch (InvalidOperationException)
        {
            // Enrollment already completed or cancelled — idempotent, ignore
        }
    }
}
