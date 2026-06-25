using MediatR;
using System.ComponentModel.DataAnnotations;

namespace DriveEase.Enrollments.Application.Commands.EnrollStudent;

public sealed record EnrollStudentCommand(
    Guid StudentId,
    Guid DrivingSchoolId,
    [Range(1.0, 100_000.0)] decimal Fee) : IRequest<Guid>;
