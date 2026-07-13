using DriveEase.Enrollments.Domain.Repositories;
using DriveEase.Enrollments.Application.Services;
using MediatR;

namespace DriveEase.Enrollments.Application.Commands.ProcessPayment;

public sealed class ProcessPaymentHandler(
    IEnrollmentRepository repository,
    IPaymentGateway paymentGateway) : IRequestHandler<ProcessPaymentCommand, bool>
{
    public async Task<bool> Handle(ProcessPaymentCommand request, CancellationToken cancellationToken)
    {
        var enrollment = await repository.GetByIdAsync(request.EnrollmentId, cancellationToken)
            ?? throw new InvalidOperationException($"Enrollment {request.EnrollmentId} not found.");

        if (enrollment.StudentId != request.CallerStudentId)
            throw new UnauthorizedAccessException("You can only pay for your own enrollment.");

        var success = await paymentGateway.ChargeAsync(enrollment.StudentId, enrollment.Fee, cancellationToken);

        if (success)
        {
            enrollment.ConfirmPayment();
            await repository.UpdateAsync(enrollment, cancellationToken);
        }
        else
        {
            enrollment.FailPayment("Payment gateway declined the charge.");
            await repository.UpdateAsync(enrollment, cancellationToken);
        }

        return success;
    }
}
