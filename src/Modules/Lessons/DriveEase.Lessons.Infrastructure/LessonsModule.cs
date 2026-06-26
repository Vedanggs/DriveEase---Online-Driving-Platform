using DriveEase.Lessons.Domain.Repositories;
using DriveEase.Lessons.Infrastructure.Persistence;
using DriveEase.Lessons.Infrastructure.Workers;
using DriveEase.Shared.Outbox;
using Microsoft.EntityFrameworkCore;
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
                opt.UseSqlServer(connectionString);

            opt.AddInterceptors(sp.GetRequiredService<OutboxInterceptor>());
        });

        services.AddScoped<ILessonRepository, LessonRepository>();
        services.AddScoped<IUpcomingLessonsQuery, UpcomingLessonsQuery>();

        services.AddHostedService<LessonReminderWorker>();
        services.AddHostedService<LessonExpiryWorker>();

        return services;
    }
}
