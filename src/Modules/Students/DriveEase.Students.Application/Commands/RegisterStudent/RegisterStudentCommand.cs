using MediatR;
using System.ComponentModel.DataAnnotations;

namespace DriveEase.Students.Application.Commands.RegisterStudent;

public sealed record RegisterStudentCommand(
    [Required, MaxLength(200)] string FullName,
    [Required, EmailAddress, MaxLength(200)] string Email,
    [Phone, MaxLength(30)] string PhoneNumber,
    DateOnly DateOfBirth,
    [Required, MinLength(8), MaxLength(100)] string Password) : IRequest<Guid>;
