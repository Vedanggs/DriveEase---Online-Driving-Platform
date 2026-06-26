using DriveEase.Enrollments.Domain.Repositories;
using DriveEase.Lessons.Domain.Events;
using DriveEase.Shared.Messaging;

namespace DriveEase.Enrollments.Application.EventHandlers;

public sealed class OnLessonPackageCompleted(IEnrollmentRepository repository)
    : IIntegrationEventHandler<LessonPackageCompletedEvent>
{
    public async Task HandleAsync(LessonPackageCompletedEvent evt, CancellationToken cancellationToken = default)
    {
        var enrollment = await repository.GetByIdAsync(evt.EnrollmentId, cancellationToken);
        if (enrollment is null) return;

        try
        {
            enrollment.Complete();
            await repository.UpdateAsync(enrollment, cancellationToken);
        }
        catch (InvalidOperationException)
        {
            // Enrollment already completed or cancelled — idempotent, ignore
        }
    }
}
