using DriveEase.Notifications.Application.Services;
using Microsoft.Extensions.Logging;

namespace DriveEase.Notifications.Infrastructure;

// Structured logging: named {} placeholders become searchable properties in
// Serilog/Application Insights — queryable as Channel, RecipientId, Subject, etc.
public sealed class FakeNotificationSender(ILogger<FakeNotificationSender> logger) : INotificationSender
{
    private static readonly EventId EmailSentEventId = new(1001, "NotificationEmailSent");
    private static readonly EventId SmsSentEventId   = new(1002, "NotificationSmsSent");

    public Task SendEmailAsync(
        Guid recipientId, string subject, string body,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            EmailSentEventId,
            "[NOTIFICATION] Channel={Channel} RecipientId={RecipientId} Subject={Subject} SentAt={SentAt} Body={Body}",
            "Email", recipientId, subject, DateTime.UtcNow, body);

        return Task.CompletedTask;
    }

    public Task SendSmsAsync(
        Guid recipientId, string message,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            SmsSentEventId,
            "[NOTIFICATION] Channel={Channel} RecipientId={RecipientId} SentAt={SentAt} Message={Message}",
            "SMS", recipientId, DateTime.UtcNow, message);

        return Task.CompletedTask;
    }
}
