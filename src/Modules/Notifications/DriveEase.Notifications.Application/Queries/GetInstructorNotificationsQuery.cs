using MediatR;

namespace DriveEase.Notifications.Application.Queries;

public sealed record InstructorNotificationDto(
    Guid Id,
    string Type,
    string StudentName,
    string Detail,
    DateTime? ScheduledAt,
    DateTime CreatedAt,
    bool IsRead);

public sealed record GetInstructorNotificationsQuery(Guid InstructorId)
    : IRequest<IReadOnlyList<InstructorNotificationDto>>;
