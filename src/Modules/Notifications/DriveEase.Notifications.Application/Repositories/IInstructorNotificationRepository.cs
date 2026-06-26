using DriveEase.Notifications.Domain.Entities;

namespace DriveEase.Notifications.Application.Repositories;

public interface IInstructorNotificationRepository
{
    Task AddAsync(InstructorNotification notification, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<InstructorNotification>> GetByInstructorAsync(Guid instructorId, CancellationToken cancellationToken = default);
    Task MarkReadAsync(Guid notificationId, CancellationToken cancellationToken = default);
}
