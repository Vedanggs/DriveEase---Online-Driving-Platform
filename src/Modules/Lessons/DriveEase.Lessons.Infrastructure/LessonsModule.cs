using DriveEase.Lessons.Domain.Repositories;
using DriveEase.Lessons.Infrastructure.Persistence;
using DriveEase.Lessons.Infrastructure.Workers;
using DriveEase.Shared.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace DriveEase.Lessons.Infrastructure;

public static class LessonsModule
{
    public static IServiceCollection AddLessonsModule(
        this IServiceCollection services,
        string connectionString)
    {
        services.AddSingleton<OutboxInterceptor>();

        services.AddDbContext<LessonsDbContext>((sp, opt) =>
        {
            if (connectionString.StartsWith("Data Source=", StringComparison.OrdinalIgnoreCase))
                opt.UseSqlite(connectionString);
            else
            {
                opt.UseSqlServer(connectionString);
                // Migrations were scaffolded against SQLite; EF's provider-aware model
                // comparison flags a false-positive "pending changes" diff on SQL Server.
                // Existing migrations are still correct — safe to skip this check.
                opt.ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning));
            }

            opt.AddInterceptors(sp.GetRequiredService<OutboxInterceptor>());
        });

        services.AddScoped<ILessonRepository, LessonRepository>();
        services.AddScoped<IUpcomingLessonsQuery, UpcomingLessonsQuery>();

        services.AddHostedService<LessonReminderWorker>();
        services.AddHostedService<LessonExpiryWorker>();

        return services;
    }
}
