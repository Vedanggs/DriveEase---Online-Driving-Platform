using DriveEase.Schools.Application.Queries.GetSchoolInstructors;
using DriveEase.Schools.Domain.Repositories;
using MediatR;

namespace DriveEase.Schools.Application.Queries.GetInstructorById;

public sealed record GetInstructorByIdQuery(Guid InstructorId) : IRequest<InstructorDto?>;

public sealed class GetInstructorByIdHandler(IInstructorRepository repository)
    : IRequestHandler<GetInstructorByIdQuery, InstructorDto?>
{
    public async Task<InstructorDto?> Handle(GetInstructorByIdQuery request, CancellationToken cancellationToken)
    {
        var instructor = await repository.GetByIdAsync(request.InstructorId, cancellationToken);
        return instructor is null ? null
            : new InstructorDto(instructor.Id, instructor.FullName, instructor.LicenseNumber, instructor.IsAvailable);
    }
}
