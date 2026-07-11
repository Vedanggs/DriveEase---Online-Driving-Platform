using DriveEase.Lessons.Domain.Events;
using DriveEase.Notifications.Application.Repositories;
using DriveEase.Notifications.Application.Services;
using DriveEase.Notifications.Domain.Entities;
using DriveEase.Shared.Messaging;

namespace DriveEase.Notifications.Application.EventHandlers;

public sealed class OnLessonCancelled(
    INotificationSender sender,
    IInstructorNotificationRepository notificationRepo)
    : IIntegrationEventHandler<LessonCancelledEvent>
{
    public async Task HandleAsync(LessonCancelledEvent evt, CancellationToken cancellationToken = default)
    {
        await sender.SendEmailAsync(
            evt.StudentId,
            subject: "Lesson Cancelled",
            body: $"Your driving lesson scheduled for {evt.ScheduledAt:f} (UTC) has been cancelled. " +
                  "You can book a new lesson any time.",
            cancellationToken);

        var notification = InstructorNotification.Create(
            instructorId: evt.InstructorId,
            type: "lesson",
            studentName: evt.StudentName,
            detail: "cancelled a lesson",
            scheduledAt: evt.ScheduledAt);

        await notificationRepo.AddAsync(notification, cancellationToken);
    }
}
