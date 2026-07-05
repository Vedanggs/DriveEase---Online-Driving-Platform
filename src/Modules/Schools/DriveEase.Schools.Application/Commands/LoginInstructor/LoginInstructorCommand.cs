using MediatR;

namespace DriveEase.Schools.Application.Commands.LoginInstructor;

public sealed record LoginInstructorCommand(
    string Email,
    string Password) : IRequest<LoginInstructorResultDto?>;

public sealed record LoginInstructorResultDto(Guid InstructorId, Guid SchoolId, string SchoolName, string FullName, string Email);
