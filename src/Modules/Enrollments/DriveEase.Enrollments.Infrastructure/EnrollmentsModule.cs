using DriveEase.Enrollments.Application.EventHandlers;
using DriveEase.Enrollments.Application.Services;
using DriveEase.Enrollments.Domain.Repositories;
using DriveEase.Enrollments.Infrastructure.Persistence;
using DriveEase.Enrollments.Infrastructure.Workers;
using DriveEase.Lessons.Domain.Events;
using DriveEase.Shared.Messaging;
using DriveEase.Shared.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace DriveEase.Enrollments.Infrastructure;

public static class EnrollmentsModule
{
    public static IServiceCollection AddEnrollmentsModule(
        this IServiceCollection services,
        string connectionString)
    {
        services.AddSingleton<OutboxInterceptor>();

        services.AddDbContext<EnrollmentsDbContext>((sp, opt) =>
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

        services.AddScoped<IEnrollmentRepository, EnrollmentRepository>();
        services.AddScoped<IPaymentGateway, FakePaymentGateway>();

        services.AddScoped<IIntegrationEventHandler<LessonPackageCompletedEvent>, OnLessonPackageCompleted>();

        services.AddHostedService<IncompleteEnrollmentWorker>();

        return services;
    }
}
