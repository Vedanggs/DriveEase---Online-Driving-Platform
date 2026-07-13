using DriveEase.Enrollments.Infrastructure.Persistence;
using DriveEase.Lessons.Infrastructure.Persistence;
using DriveEase.Shared.Messaging;
using DriveEase.Shared.Outbox;
using Microsoft.EntityFrameworkCore;
using System.Reflection;
using System.Text.Json;

namespace DriveEase.Api.Workers;

// Polls the outbox tables in each module every 10 seconds.
// On success: stamps ProcessedAt so the message is never picked up again.
// On failure: increments RetryCount. After MaxRetryCount failures the message
//             is dead-lettered (DeadLettered = true) and permanently skipped.
//
// NOTE: for true exponential back-off, add a NextRetryAt column and change the
//       WHERE clause to also check `m.NextRetryAt <= DateTime.UtcNow`. The current
//       implementation retries on every 10-second poll until MaxRetryCount is hit.
public sealed class OutboxRelayWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<OutboxRelayWorker> logger) : BackgroundService
{
    private const int MaxRetryCount = 5;

    private static readonly MethodInfo PublishMethod =
        typeof(IEventBus).GetMethod(nameof(IEventBus.PublishAsync))!;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            // A transient failure (e.g. a SQL timeout when the DB is cold) must never
            // escape this loop — an unhandled exception here permanently stops the
            // BackgroundService, silently killing outbox relay (and thus all
            // notifications) until the app is restarted. Log and retry next tick instead.
            try
            {
                await RelayAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Outbox relay cycle failed; will retry on the next poll.");
            }

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private async Task RelayAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var eventBus = scope.ServiceProvider.GetRequiredService<IEventBus>();

        await ProcessContextAsync(
            scope.ServiceProvider.GetRequiredService<EnrollmentsDbContext>(),
            eventBus, cancellationToken);

        await ProcessContextAsync(
            scope.ServiceProvider.GetRequiredService<LessonsDbContext>(),
            eventBus, cancellationToken);
    }

    private async Task ProcessContextAsync(
        DbContext context,
        IEventBus eventBus,
        CancellationToken cancellationToken)
    {
        var pending = await context.Set<OutboxMessage>()
            .AsNoTracking()
            .Where(m => m.ProcessedAt == null && !m.DeadLettered)
            .OrderBy(m => m.CreatedAt)
            .Take(50)
            .ToListAsync(cancellationToken);

        foreach (var message in pending)
        {
            try
            {
                var eventType = Type.GetType(message.EventType)
                    ?? throw new InvalidOperationException($"Cannot resolve event type '{message.EventType}'.");

                var integrationEvent = JsonSerializer.Deserialize(message.Payload, eventType)!;

                await (Task)PublishMethod
                    .MakeGenericMethod(eventType)
                    .Invoke(eventBus, [integrationEvent, cancellationToken])!;

                logger.LogInformation("Relayed outbox message {Id} ({EventType})", message.Id, eventType.Name);

                var processedAt = DateTime.UtcNow;
                await context.Set<OutboxMessage>()
                    .Where(m => m.Id == message.Id)
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(m => m.ProcessedAt, processedAt)
                        .SetProperty(m => m.Error, (string?)null),
                        cancellationToken);
            }
            catch (Exception ex)
            {
                var newRetryCount = message.RetryCount + 1;
                var deadLetter    = newRetryCount >= MaxRetryCount;

                logger.LogError(ex,
                    "Failed to relay outbox message {Id} (attempt {Attempt}/{Max})",
                    message.Id, newRetryCount, MaxRetryCount);

                if (deadLetter)
                    logger.LogWarning(
                        "Dead-lettering outbox message {Id} after {Max} failed attempts",
                        message.Id, MaxRetryCount);

                await context.Set<OutboxMessage>()
                    .Where(m => m.Id == message.Id)
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(m => m.RetryCount,    newRetryCount)
                        .SetProperty(m => m.Error,         ex.Message)
                        .SetProperty(m => m.DeadLettered,  deadLetter),
                        cancellationToken);
            }
        }
    }
}
