using DriveEase.Notifications.Application.Repositories;
using MediatR;

namespace DriveEase.Notifications.Application.Queries;

public sealed class GetInstructorNotificationsHandler(IInstructorNotificationRepository repository)
    : IRequestHandler<GetInstructorNotificationsQuery, IReadOnlyList<InstructorNotificationDto>>
{
    public async Task<IReadOnlyList<InstructorNotificationDto>> Handle(
        GetInstructorNotificationsQuery request, CancellationToken cancellationToken)
    {
        var notifications = await repository.GetByInstructorAsync(request.InstructorId, cancellationToken);

        return notifications
            .Select(n => new InstructorNotificationDto(
                n.Id,
                n.Type,
                n.StudentName,
                n.Detail,
                n.CreatedAt,
                n.IsRead))
            .ToList();
    }
}
