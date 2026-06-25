using DriveEase.Schools.Domain.Repositories;
using MediatR;

namespace DriveEase.Schools.Application.Queries.GetSchoolInstructors;

public sealed class GetSchoolInstructorsHandler(IInstructorRepository repository)
    : IRequestHandler<GetSchoolInstructorsQuery, IReadOnlyList<InstructorDto>>
{
    public async Task<IReadOnlyList<InstructorDto>> Handle(
        GetSchoolInstructorsQuery request, CancellationToken cancellationToken)
    {
        var instructors = await repository.GetAvailableBySchoolAsync(request.SchoolId, cancellationToken);

        return instructors
            .Select(i => new InstructorDto(i.Id, i.FullName, i.LicenseNumber, i.IsAvailable))
            .ToList();
    }
}
