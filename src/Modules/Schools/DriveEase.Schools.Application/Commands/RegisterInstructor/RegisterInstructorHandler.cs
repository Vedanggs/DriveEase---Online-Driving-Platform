using DriveEase.Schools.Domain.Entities;
using DriveEase.Schools.Domain.Repositories;
using DriveEase.Schools.Application;
using MediatR;

namespace DriveEase.Schools.Application.Commands.RegisterInstructor;

public sealed class RegisterInstructorHandler(
    IInstructorRepository repository,
    IPasswordHasher passwordHasher)
    : IRequestHandler<RegisterInstructorCommand, Guid>
{
    public async Task<Guid> Handle(RegisterInstructorCommand request, CancellationToken cancellationToken)
    {
        var existing = await repository.GetByEmailAsync(request.Email, cancellationToken);
        if (existing is not null)
            throw new InvalidOperationException($"Instructor with email '{request.Email}' already exists.");

        var passwordHash = passwordHasher.Hash(request.Password);
        var instructor = Instructor.Create(
            request.SchoolId,
            request.FullName,
            request.LicenseNumber,
            request.Email,
            passwordHash);

        await repository.AddAsync(instructor, cancellationToken);
        return instructor.Id;
    }
}
