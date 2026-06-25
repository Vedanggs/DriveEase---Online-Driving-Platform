using DriveEase.Enrollments.Infrastructure.Persistence;
using DriveEase.Lessons.Infrastructure.Persistence;
using DriveEase.Shared.Messaging;
using DriveEase.Shared.Outbox;
using Microsoft.EntityFrameworkCore;
using System.Reflection;
using System.Text.Json;

namespace DriveEase.Api.Workers;

// Polls the outbox tables in each module every 10 seconds.
// For each unprocessed message it deserialises the event and publishes it to the event bus,
// then stamps ProcessedAt so the message is not retried.
public sealed class OutboxRelayWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<OutboxRelayWorker> logger) : BackgroundService
{
    private static readonly MethodInfo PublishMethod =
        typeof(IEventBus).GetMethod(nameof(IEventBus.PublishAsync))!;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await RelayAsync(stoppingToken);
            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
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
        // AsNoTracking: we update via ExecuteUpdateAsync to avoid EF concurrency tracking issues.
        var pending = await context.Set<OutboxMessage>()
            .AsNoTracking()
            .Where(m => m.ProcessedAt == null)
            .OrderBy(m => m.CreatedAt)
            .Take(50)
            .ToListAsync(cancellationToken);

        foreach (var message in pending)
        {
            string? error = null;
            try
            {
                var eventType = Type.GetType(message.EventType)
                    ?? throw new InvalidOperationException($"Cannot resolve event type '{message.EventType}'.");

                var integrationEvent = JsonSerializer.Deserialize(message.Payload, eventType)!;

                await (Task)PublishMethod
                    .MakeGenericMethod(eventType)
                    .Invoke(eventBus, [integrationEvent, cancellationToken])!;

                logger.LogInformation("Relayed outbox message {Id} ({EventType})", message.Id, eventType.Name);
            }
            catch (Exception ex)
            {
                error = ex.Message;
                logger.LogError(ex, "Failed to relay outbox message {Id}", message.Id);
            }

            // Direct update — no EF change tracker involved, no concurrency exception.
            var processedAt = DateTime.UtcNow;
            await context.Set<OutboxMessage>()
                .Where(m => m.Id == message.Id)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(m => m.ProcessedAt, processedAt)
                    .SetProperty(m => m.Error, error),
                    cancellationToken);
        }
    }
}
