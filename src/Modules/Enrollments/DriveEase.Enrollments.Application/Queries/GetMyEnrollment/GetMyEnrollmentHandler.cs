using DriveEase.Enrollments.Application.DTOs;
using DriveEase.Enrollments.Domain.Repositories;
using DriveEase.Lessons.Domain.Repositories;
using MediatR;

namespace DriveEase.Enrollments.Application.Queries.GetMyEnrollment;

public sealed class GetMyEnrollmentHandler(
    IEnrollmentRepository repository,
    ILessonRepository lessonRepository)
    : IRequestHandler<GetMyEnrollmentQuery, EnrollmentDto?>
{
    private const int MaxLessonsPerEnrollment = 5;

    public async Task<EnrollmentDto?> Handle(GetMyEnrollmentQuery request, CancellationToken cancellationToken)
    {
        var enrollment = await repository.GetActiveByStudentIdAsync(request.StudentId, cancellationToken);
        if (enrollment is null) return null;

        // If all lessons for this enrollment are completed, auto-complete it so the student can re-enroll.
        // This handles cases where the completion event was missed (e.g. server restart, existing data).
        var completedCount = await lessonRepository.CountCompletedByEnrollmentAsync(enrollment.Id, cancellationToken);
        if (completedCount >= MaxLessonsPerEnrollment)
        {
            try
            {
                enrollment.Complete();
                await repository.UpdateAsync(enrollment, cancellationToken);
            }
            catch (InvalidOperationException) { /* already completed */ }
            return null;
        }

        return new EnrollmentDto(
            enrollment.Id,
            enrollment.StudentId,
            enrollment.DrivingSchoolId,
            enrollment.InstructorId,
            enrollment.Fee,
            enrollment.PaymentStatus.ToString(),
            enrollment.Status.ToString(),
            enrollment.EnrolledAt,
            enrollment.PaymentConfirmedAt);
    }
}
