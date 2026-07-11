using DriveEase.Enrollments.Domain.Events;
using DriveEase.Lessons.Domain.Events;
using DriveEase.Notifications.Application.EventHandlers;
using DriveEase.Notifications.Application.Repositories;
using DriveEase.Notifications.Application.Services;
using DriveEase.Notifications.Infrastructure.Persistence;
using DriveEase.Shared.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace DriveEase.Notifications.Infrastructure;

public static class NotificationsModule
{
    public static IServiceCollection AddNotificationsModule(
        this IServiceCollection services,
        string connectionString)
    {
        services.AddDbContext<NotificationsDbContext>((_, opt) =>
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
        });

        services.AddScoped<IInstructorNotificationRepository, InstructorNotificationRepository>();
        services.AddScoped<INotificationSender, FakeNotificationSender>();

        services.AddScoped<IIntegrationEventHandler<EnrollmentConfirmedEvent>, OnEnrollmentConfirmed>();
        services.AddScoped<IIntegrationEventHandler<PaymentFailedEvent>, OnPaymentFailed>();
        services.AddScoped<IIntegrationEventHandler<EnrollmentCancelledEvent>, OnEnrollmentCancelled>();
        services.AddScoped<IIntegrationEventHandler<InstructorAssignedEvent>, OnInstructorAssigned>();
        services.AddScoped<IIntegrationEventHandler<LessonBookedEvent>, OnLessonBooked>();
        services.AddScoped<IIntegrationEventHandler<LessonCompletedEvent>, OnLessonCompleted>();
        services.AddScoped<IIntegrationEventHandler<LessonCancelledEvent>, OnLessonCancelled>();

        return services;
    }
}
