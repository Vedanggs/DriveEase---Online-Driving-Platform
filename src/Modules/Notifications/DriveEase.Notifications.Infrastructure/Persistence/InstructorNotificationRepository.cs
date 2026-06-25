using DriveEase.Notifications.Application.Repositories;
using DriveEase.Notifications.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DriveEase.Notifications.Infrastructure.Persistence;

public sealed class InstructorNotificationRepository(NotificationsDbContext dbContext)
    : IInstructorNotificationRepository
{
    public async Task AddAsync(InstructorNotification notification, CancellationToken cancellationToken = default)
    {
        await dbContext.InstructorNotifications.AddAsync(notification, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<InstructorNotification>> GetByInstructorAsync(
        Guid instructorId, CancellationToken cancellationToken = default) =>
        await dbContext.InstructorNotifications
            .AsNoTracking()
            .Where(n => n.InstructorId == instructorId)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync(cancellationToken);
}
