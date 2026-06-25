using DriveEase.Enrollments.Domain.Events;
using DriveEase.Notifications.Application.Repositories;
using DriveEase.Notifications.Application.Services;
using DriveEase.Notifications.Domain.Entities;
using DriveEase.Shared.Messaging;

namespace DriveEase.Notifications.Application.EventHandlers;

public sealed class OnInstructorAssigned(
    INotificationSender sender,
    IInstructorNotificationRepository notificationRepo)
    : IIntegrationEventHandler<InstructorAssignedEvent>
{
    public async Task HandleAsync(InstructorAssignedEvent evt, CancellationToken cancellationToken = default)
    {
        await sender.SendEmailAsync(
            evt.StudentId,
            subject: "Instructor Assigned",
            body: $"Great news! An instructor has been assigned to your enrollment {evt.EnrollmentId}. " +
                  "You can now book your first lesson.",
            cancellationToken);

        var notification = InstructorNotification.Create(
            instructorId: evt.InstructorId,
            type: "enrollment",
            studentName: "A student",
            detail: $"enrolled and was assigned to you (enrollment {evt.EnrollmentId.ToString()[..8]}...)");

        await notificationRepo.AddAsync(notification, cancellationToken);
    }
}
