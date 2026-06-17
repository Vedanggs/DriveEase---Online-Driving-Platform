using DriveEase.Enrollments.Application.Services;
using DriveEase.Enrollments.Domain.Repositories;
using DriveEase.Enrollments.Infrastructure.Persistence;
using DriveEase.Enrollments.Infrastructure.Workers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DriveEase.Enrollments.Infrastructure;

public static class EnrollmentsModule
{
    public static IServiceCollection AddEnrollmentsModule(
        this IServiceCollection services,
        string connectionString)
    {
        services.AddDbContext<EnrollmentsDbContext>(opt =>
        {
            if (connectionString.StartsWith("Data Source=", StringComparison.OrdinalIgnoreCase))
                opt.UseSqlite(connectionString);
            else
                opt.UseSqlServer(connectionString);
        });

        services.AddScoped<IEnrollmentRepository, EnrollmentRepository>();
        services.AddScoped<IPaymentGateway, FakePaymentGateway>();

        services.AddHostedService<IncompleteEnrollmentWorker>();

        return services;
    }
}
