using MediatR;
using System.ComponentModel.DataAnnotations;

namespace DriveEase.Schools.Application.Commands.RegisterSchool;

public sealed record RegisterSchoolCommand(
    [property: Required, MaxLength(200)] string Name,
    [property: Required, MaxLength(500)] string Address,
    [property: Required, EmailAddress, MaxLength(200)] string ContactEmail) : IRequest<Guid>;
