using DriveEase.Lessons.Domain.Events;
using DriveEase.Notifications.Application.Repositories;
using DriveEase.Notifications.Application.Services;
using DriveEase.Notifications.Domain.Entities;
using DriveEase.Shared.Messaging;

namespace DriveEase.Notifications.Application.EventHandlers;

public sealed class OnLessonBooked(
    INotificationSender sender,
    IInstructorNotificationRepository notificationRepo)
    : IIntegrationEventHandler<LessonBookedEvent>
{
    public async Task HandleAsync(LessonBookedEvent evt, CancellationToken cancellationToken = default)
    {
        await sender.SendEmailAsync(
            evt.StudentId,
            subject: "Lesson Booked",
            body: $"Your driving lesson is confirmed for {evt.ScheduledAt:f} (UTC). " +
                  "You'll receive a reminder 24 hours before.",
            cancellationToken);

        // ScheduledAt is stored raw (UTC) and formatted client-side via formatScheduledAt,
        // same as the Upcoming/History tabs — baking a pre-formatted "... at h:mm tt UTC"
        // string here made this notification show a different time than the rest of the
        // dashboard, since the frontend has no way to convert an already-rendered string.
        var notification = InstructorNotification.Create(
            instructorId: evt.InstructorId,
            type: "lesson",
            studentName: evt.StudentName,
            detail: "booked a lesson",
            scheduledAt: evt.ScheduledAt);

        await notificationRepo.AddAsync(notification, cancellationToken);
    }
}
