using DriveEase.Enrollments.Application.DTOs;
using DriveEase.Enrollments.Domain.Repositories;
using MediatR;

namespace DriveEase.Enrollments.Application.Queries.GetMyEnrollment;

public sealed class GetMyEnrollmentHandler(IEnrollmentRepository repository)
    : IRequestHandler<GetMyEnrollmentQuery, EnrollmentDto?>
{
    public async Task<EnrollmentDto?> Handle(GetMyEnrollmentQuery request, CancellationToken cancellationToken)
    {
        var enrollment = await repository.GetActiveByStudentIdAsync(request.StudentId, cancellationToken);
        if (enrollment is null) return null;

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
