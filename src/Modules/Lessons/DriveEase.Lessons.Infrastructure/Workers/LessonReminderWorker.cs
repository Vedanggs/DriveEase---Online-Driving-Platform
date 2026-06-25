using DriveEase.Lessons.Domain.Events;
using DriveEase.Lessons.Domain.Repositories;
using DriveEase.Shared.Messaging;
using DriveEase.Shared.Telemetry;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace DriveEase.Lessons.Infrastructure.Workers;

public sealed class LessonReminderWorker(
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    ILogger<LessonReminderWorker> logger) : BackgroundService
{
    // Override in appsettings.Development.json as "Workers:LessonReminderIntervalSeconds": 30
    // to verify the worker fires in dev without waiting a full hour.
    private TimeSpan Interval =>
        TimeSpan.FromSeconds(
            int.TryParse(configuration["Workers:LessonReminderIntervalSeconds"], out var s) ? s : 3600);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation(
            "LessonReminderWorker started — polling every {Interval}",
            Interval);

        while (!stoppingToken.IsCancellationRequested)
        {
            await SendRemindersAsync(stoppingToken);
            await Task.Delay(Interval, stoppingToken);
        }
    }

    private async Task SendRemindersAsync(CancellationToken cancellationToken)
    {
        // Root span for this worker tick — top-level operation in App Insights.
        using var workerActivity = DriveEaseTelemetry.Source.StartActivity(
            "LessonWorker.SendReminders", ActivityKind.Internal);

        using var scope = scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IUpcomingLessonsQuery>();
        var eventBus   = scope.ServiceProvider.GetRequiredService<IEventBus>();

        // Find all lessons occurring in ~24 hours (within a 25h window checked hourly)
        var upcomingLessons = await repository.GetScheduledWithin25HoursAsync(cancellationToken);

        workerActivity?.SetTag("worker.lessons_checked", upcomingLessons.Count);

        var remindersSentThisTick = 0;
        foreach (var lesson in upcomingLessons)
        {
            var hoursUntil = (lesson.ScheduledAt - DateTime.UtcNow).TotalHours;
            if (hoursUntil is > 23 and <= 25)
            {
                // Child span per reminder — DB write + event publish are nested inside.
                using var reminderActivity = DriveEaseTelemetry.Source.StartActivity(
                    "LessonWorker.SendReminder", ActivityKind.Internal);
                reminderActivity?.SetTag("lesson.id", lesson.LessonId);
                reminderActivity?.SetTag("lesson.student_id", lesson.StudentId);
                reminderActivity?.SetTag("lesson.hours_until", hoursUntil);

                var reminderEvent = LessonBookedEvent.Create(
                    lesson.LessonId, lesson.StudentId, lesson.StudentName,
                    lesson.InstructorId, lesson.ScheduledAt);

                await eventBus.PublishAsync(reminderEvent, cancellationToken);

                reminderActivity?.AddEvent(new ActivityEvent("reminder.published"));
                reminderActivity?.SetStatus(ActivityStatusCode.Ok);

                remindersSentThisTick++;
                logger.LogInformation(
                    "Reminder sent for lesson {LessonId} (student: {StudentName})",
                    lesson.LessonId, lesson.StudentName);
            }
        }

        DriveEaseTelemetry.LessonRemindersSent.Add(remindersSentThisTick);
        workerActivity?.SetTag("worker.reminders_sent", remindersSentThisTick);

        logger.LogInformation(
            "LessonReminderWorker tick complete — {Checked} lessons checked, {Sent} reminders sent",
            upcomingLessons.Count, remindersSentThisTick);
    }
}

public record UpcomingLesson(
    Guid LessonId, Guid StudentId, string StudentName, Guid InstructorId, DateTime ScheduledAt);

public interface IUpcomingLessonsQuery
{
    Task<IReadOnlyList<UpcomingLesson>> GetScheduledWithin25HoursAsync(CancellationToken cancellationToken = default);
}
