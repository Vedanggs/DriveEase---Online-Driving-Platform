using MediatR;

namespace DriveEase.Enrollments.Application.Commands.ProcessPayment;

public sealed record ProcessPaymentCommand(Guid EnrollmentId, Guid CallerStudentId) : IRequest<bool>;
