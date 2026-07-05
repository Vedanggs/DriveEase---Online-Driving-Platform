using DriveEase.Schools.Domain.Repositories;
using DriveEase.Schools.Application;
using MediatR;

namespace DriveEase.Schools.Application.Commands.LoginInstructor;

public sealed class LoginInstructorHandler(
    IInstructorRepository repository,
    IDrivingSchoolRepository schoolRepository,
    IPasswordHasher passwordHasher)
    : IRequestHandler<LoginInstructorCommand, LoginInstructorResultDto?>
{
    public async Task<LoginInstructorResultDto?> Handle(LoginInstructorCommand request, CancellationToken cancellationToken)
    {
        var instructor = await repository.GetByEmailAsync(request.Email.ToLowerInvariant(), cancellationToken);
        if (instructor is null || string.IsNullOrEmpty(instructor.PasswordHash))
            return null;

        if (!passwordHasher.Verify(request.Password, instructor.PasswordHash))
            return null;

        var school = await schoolRepository.GetByIdAsync(instructor.SchoolId, cancellationToken);
        var schoolName = school?.Name ?? "Driving School";

        return new LoginInstructorResultDto(
            instructor.Id,
            instructor.SchoolId,
            schoolName,
            instructor.FullName,
            instructor.Email ?? request.Email);
    }
}
