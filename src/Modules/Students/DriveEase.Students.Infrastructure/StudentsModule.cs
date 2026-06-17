using DriveEase.Students.Domain.Repositories;
using DriveEase.Students.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
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
                opt.UseSqlServer(connectionString);
        });

        services.AddScoped<IStudentRepository, StudentRepository>();

        return services;
    }
}
