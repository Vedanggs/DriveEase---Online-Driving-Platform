using MediatR;
using System.ComponentModel.DataAnnotations;

namespace DriveEase.Students.Application.Commands.RegisterStudent;

public sealed record RegisterStudentCommand(
    [property: Required, MaxLength(200)] string FullName,
    [property: Required, EmailAddress, MaxLength(200)] string Email,
    [property: Phone, MaxLength(30)] string PhoneNumber,
    DateOnly DateOfBirth) : IRequest<Guid>;
