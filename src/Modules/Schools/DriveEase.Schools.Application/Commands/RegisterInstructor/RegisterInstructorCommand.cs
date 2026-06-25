using MediatR;

namespace DriveEase.Schools.Application.Commands.RegisterInstructor;

public sealed record RegisterInstructorCommand(
    Guid SchoolId,
    string FullName,
    string LicenseNumber) : IRequest<Guid>;
