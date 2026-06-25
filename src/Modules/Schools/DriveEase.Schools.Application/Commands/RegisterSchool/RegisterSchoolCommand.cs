using MediatR;
using System.ComponentModel.DataAnnotations;

namespace DriveEase.Schools.Application.Commands.RegisterSchool;

public sealed record RegisterSchoolCommand(
    [Required, MaxLength(200)] string Name,
    [Required, MaxLength(500)] string Address,
    [Required, EmailAddress, MaxLength(200)] string ContactEmail) : IRequest<Guid>;
