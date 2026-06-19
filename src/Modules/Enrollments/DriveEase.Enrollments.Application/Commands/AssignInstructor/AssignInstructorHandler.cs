using DriveEase.Enrollments.Domain.Repositories;
using MediatR;

namespace DriveEase.Enrollments.Application.Commands.AssignInstructor;

public sealed class AssignInstructorHandler(
    IEnrollmentRepository repository) : IRequestHandler<AssignInstructorCommand>
{
    public async Task Handle(AssignInstructorCommand request, CancellationToken cancellationToken)
    {
        var enrollment = await repository.GetByIdAsync(request.EnrollmentId, cancellationToken)
            ?? throw new InvalidOperationException($"Enrollment {request.EnrollmentId} not found.");

        enrollment.AssignInstructor(request.InstructorId);
        await repository.UpdateAsync(enrollment, cancellationToken);
    }
}
