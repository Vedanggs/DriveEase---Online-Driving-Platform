using DriveEase.Notifications.Application.Repositories;
using MediatR;

namespace DriveEase.Notifications.Application.Commands;

public sealed record MarkNotificationReadCommand(Guid NotificationId) : IRequest;

public sealed class MarkNotificationReadHandler(IInstructorNotificationRepository repository)
    : IRequestHandler<MarkNotificationReadCommand>
{
    public async Task Handle(MarkNotificationReadCommand request, CancellationToken cancellationToken)
        => await repository.MarkReadAsync(request.NotificationId, cancellationToken);
}
