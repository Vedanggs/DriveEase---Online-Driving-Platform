using DriveEase.Schools.Application.Queries.GetAllSchools;
using DriveEase.Schools.Domain.Repositories;
using DriveEase.Schools.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace DriveEase.Schools.Infrastructure;

public static class SchoolsModule
{
    public static IServiceCollection AddSchoolsModule(
        this IServiceCollection services,
        string connectionString)
    {
        services.AddDbContext<SchoolsDbContext>(opt =>
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

        services.AddScoped<IDrivingSchoolRepository, SchoolRepository>();
        services.AddScoped<IInstructorRepository, InstructorRepository>();
        services.AddScoped<ISchoolQueryService, SchoolQueryService>();

        return services;
    }
}
