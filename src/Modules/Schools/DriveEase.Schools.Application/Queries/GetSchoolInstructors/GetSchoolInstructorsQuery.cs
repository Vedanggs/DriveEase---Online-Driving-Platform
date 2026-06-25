using MediatR;

namespace DriveEase.Schools.Application.Queries.GetSchoolInstructors;

public sealed record InstructorDto(Guid Id, string FullName, string LicenseNumber, bool IsAvailable);

public sealed record GetSchoolInstructorsQuery(Guid SchoolId)
    : IRequest<IReadOnlyList<InstructorDto>>;
