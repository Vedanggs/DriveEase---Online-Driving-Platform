using DriveEase.Enrollments.Application.EventHandlers;
using DriveEase.Enrollments.Application.Services;
using DriveEase.Enrollments.Domain.Repositories;
using DriveEase.Enrollments.Infrastructure.Persistence;
using DriveEase.Enrollments.Infrastructure.Workers;
using DriveEase.Lessons.Domain.Events;
using DriveEase.Shared.Messaging;
using DriveEase.Shared.Outbox;
using Microsoft.EntityFrameworkCore;
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
                opt.UseSqlServer(connectionString);

            opt.AddInterceptors(sp.GetRequiredService<OutboxInterceptor>());
        });

        services.AddScoped<IEnrollmentRepository, EnrollmentRepository>();
        services.AddScoped<IPaymentGateway, FakePaymentGateway>();

        services.AddScoped<IIntegrationEventHandler<LessonPackageCompletedEvent>, OnLessonPackageCompleted>();

        services.AddHostedService<IncompleteEnrollmentWorker>();

        return services;
    }
}
