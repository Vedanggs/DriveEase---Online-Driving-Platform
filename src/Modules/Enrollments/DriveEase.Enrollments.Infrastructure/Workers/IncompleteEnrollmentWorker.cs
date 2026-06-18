using DriveEase.Enrollments.Domain.Repositories;
using DriveEase.Enrollments.Domain.Events;
using DriveEase.Shared.Messaging;
using DriveEase.Shared.Telemetry;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace DriveEase.Enrollments.Infrastructure.Workers;

public sealed class IncompleteEnrollmentWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<IncompleteEnrollmentWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await ProcessStaleEnrollmentsAsync(stoppingToken);
            await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
        }
    }

    private async Task ProcessStaleEnrollmentsAsync(CancellationToken cancellationToken)
    {
        // Root span for this worker tick — appears as a top-level operation in App Insights.
        using var workerActivity = DriveEaseTelemetry.Source.StartActivity(
            "EnrollmentWorker.ProcessStale", ActivityKind.Internal);

        using var scope = scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IEnrollmentRepository>();
        var eventBus   = scope.ServiceProvider.GetRequiredService<IEventBus>();

        // Auto-cancel enrollments with failed payment after 72 hours
        var stale = await repository.GetPendingPaymentOlderThanAsync(TimeSpan.FromHours(72), cancellationToken);

        workerActivity?.SetTag("worker.stale_count", stale.Count);

        foreach (var enrollment in stale)
        {
            // Child span per cancelled enrollment — stitches into the worker root span.
            using var cancelActivity = DriveEaseTelemetry.Source.StartActivity(
                "EnrollmentWorker.CancelEnrollment", ActivityKind.Internal);
            cancelActivity?.SetTag("enrollment.id", enrollment.Id);

            try
            {
                enrollment.Cancel("Auto-cancelled: payment not received within 72 hours.");
                await repository.UpdateAsync(enrollment, cancellationToken);

                var cancelledEvent = (EnrollmentCancelledEvent)enrollment.DomainEvents
                    .First(e => e is EnrollmentCancelledEvent);
                await eventBus.PublishAsync(cancelledEvent, cancellationToken);

                enrollment.ClearDomainEvents();

                DriveEaseTelemetry.EnrollmentsAutoCancelled.Add(1,
                    new TagList { { "enrollment.id", enrollment.Id.ToString() } });

                cancelActivity?.SetStatus(ActivityStatusCode.Ok);
                logger.LogInformation("Auto-cancelled stale enrollment {EnrollmentId}", enrollment.Id);
            }
            catch (Exception ex)
            {
                cancelActivity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                cancelActivity?.SetTag("error", true);
                cancelActivity?.SetTag("exception.type", ex.GetType().Name);
                cancelActivity?.SetTag("exception.message", ex.Message);
                logger.LogError(ex, "Failed to auto-cancel enrollment {EnrollmentId}", enrollment.Id);
            }
        }
    }
}
