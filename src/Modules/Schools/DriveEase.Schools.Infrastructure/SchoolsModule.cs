using DriveEase.Schools.Application.Queries.GetAllSchools;
using DriveEase.Schools.Domain.Repositories;
using DriveEase.Schools.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
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
                opt.UseSqlServer(connectionString);
        });

        services.AddScoped<IDrivingSchoolRepository, SchoolRepository>();
        services.AddScoped<IInstructorRepository, InstructorRepository>();
        services.AddScoped<ISchoolQueryService, SchoolQueryService>();

        return services;
    }
}
