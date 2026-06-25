using DriveEase.Schools.Domain.Entities;
using DriveEase.Schools.Domain.Repositories;
using MediatR;

namespace DriveEase.Schools.Application.Commands.RegisterInstructor;

public sealed class RegisterInstructorHandler(IInstructorRepository repository)
    : IRequestHandler<RegisterInstructorCommand, Guid>
{
    public async Task<Guid> Handle(RegisterInstructorCommand request, CancellationToken cancellationToken)
    {
        var instructor = Instructor.Create(request.SchoolId, request.FullName, request.LicenseNumber);
        await repository.AddAsync(instructor, cancellationToken);
        return instructor.Id;
    }
}
