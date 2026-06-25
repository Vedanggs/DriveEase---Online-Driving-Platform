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

        var notification = InstructorNotification.Create(
            instructorId: evt.InstructorId,
            type: "lesson",
            studentName: evt.StudentName,
            detail: $"booked a lesson for {evt.ScheduledAt:ddd, dd MMM} at {evt.ScheduledAt:h:mm tt} UTC");

        await notificationRepo.AddAsync(notification, cancellationToken);
    }
}
