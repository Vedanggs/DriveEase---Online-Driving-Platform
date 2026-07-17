using DriveEase.Lessons.Domain.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DriveEase.Lessons.Infrastructure.Workers;

/// <summary>
/// Runs every 30 minutes and auto-cancels lessons that are still "Scheduled"
/// but whose scheduled window has long since passed (start + max duration + 2hr grace).
/// This prevents a student's lesson slot from being permanently blocked when an
/// instructor simply never acts on the booking.
/// </summary>
public sealed class LessonExpiryWorker(
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    ILogger<LessonExpiryWorker> logger) : BackgroundService
{
    // A lesson is considered expired 30 minutes after its scheduled end time
    // if the instructor still hasn't marked it complete.
    private static readonly TimeSpan GracePeriod = TimeSpan.FromMinutes(30);

    private TimeSpan Interval =>
        TimeSpan.FromSeconds(
            int.TryParse(configuration["Workers:LessonExpiryIntervalSeconds"], out var s) ? s : 1800);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("LessonExpiryWorker started — polling every {Interval}", Interval);

        while (!stoppingToken.IsCancellationRequested)
        {
            // A transient failure must never escape this loop — an unhandled exception
            // permanently stops the BackgroundService until the app restarts.
            try
            {
                await ExpireOverdueLessonsAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "LessonExpiryWorker tick failed; will retry on the next poll.");
            }

            try
            {
                await Task.Delay(Interval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private async Task ExpireOverdueLessonsAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var repository  = scope.ServiceProvider.GetRequiredService<ILessonRepository>();

        // ScheduledAt is stored in UTC (the frontend converts to UTC via toISOString()
        // before sending). Compare against UtcNow — using local DateTime.Now here would
        // shift the comparison by the server's timezone offset and expire lessons early
        // on any non-UTC machine (e.g. an IST dev box cancelling lessons ~5.5h too soon).
        var now = DateTime.UtcNow;

        // DB: fetch all Scheduled lessons whose start time is already in the past.
        // Duration cannot be used in EF/SQLite LINQ, so we load candidates and
        // apply the end-time + grace check in memory.
        var candidates = await repository.GetExpiredScheduledAsync(now, cancellationToken);
        if (candidates.Count == 0) return;

        var expired = candidates
            .Where(l => l.ScheduledAt.Add(l.Duration).Add(GracePeriod) < now)
            .ToList();

        if (expired.Count == 0) return;

        foreach (var lesson in expired)
            lesson.Cancel();

        await repository.UpdateBatchAsync(expired, cancellationToken);

        logger.LogInformation(
            "LessonExpiryWorker: auto-cancelled {Count} overdue lesson(s)",
            expired.Count);
    }
}
