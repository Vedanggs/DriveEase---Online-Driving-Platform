using DriveEase.Enrollments.Domain.Repositories;
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
        using var workerActivity = DriveEaseTelemetry.Source.StartActivity(
            "EnrollmentWorker.ProcessStale", ActivityKind.Internal);

        using var scope = scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IEnrollmentRepository>();

        var stale = await repository.GetPendingPaymentOlderThanAsync(TimeSpan.FromHours(72), cancellationToken);

        workerActivity?.SetTag("worker.stale_count", stale.Count);

        foreach (var enrollment in stale)
        {
            using var cancelActivity = DriveEaseTelemetry.Source.StartActivity(
                "EnrollmentWorker.CancelEnrollment", ActivityKind.Internal);
            cancelActivity?.SetTag("enrollment.id", enrollment.Id);

            try
            {
                enrollment.Cancel("Auto-cancelled: payment not received within 72 hours.");
                await repository.UpdateAsync(enrollment, cancellationToken);
                // OutboxInterceptor captures EnrollmentCancelledEvent atomically during UpdateAsync

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
