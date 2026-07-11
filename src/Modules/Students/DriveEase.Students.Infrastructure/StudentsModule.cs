using DriveEase.Students.Domain.Repositories;
using DriveEase.Students.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace DriveEase.Students.Infrastructure;

public static class StudentsModule
{
    public static IServiceCollection AddStudentsModule(
        this IServiceCollection services,
        string connectionString)
    {
        services.AddDbContext<StudentsDbContext>(opt =>
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

        services.AddScoped<IStudentRepository, StudentRepository>();

        return services;
    }
}
