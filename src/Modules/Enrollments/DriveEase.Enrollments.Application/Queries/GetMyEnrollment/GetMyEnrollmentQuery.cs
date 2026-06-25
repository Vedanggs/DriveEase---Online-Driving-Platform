using DriveEase.Enrollments.Application.DTOs;
using MediatR;

namespace DriveEase.Enrollments.Application.Queries.GetMyEnrollment;

public sealed record GetMyEnrollmentQuery(Guid StudentId) : IRequest<EnrollmentDto?>;
