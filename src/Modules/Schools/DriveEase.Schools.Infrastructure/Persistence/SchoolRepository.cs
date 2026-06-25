using Dapper;
using DriveEase.Schools.Domain.Entities;
using DriveEase.Schools.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace DriveEase.Schools.Infrastructure.Persistence;

public sealed class SchoolRepository(SchoolsDbContext dbContext) : IDrivingSchoolRepository
{
    // Dapper row type — uses CLR types that map correctly for both SQLite and SQL Server.
    // SQLite: TEXT→string, INTEGER→bool, TEXT(datetime)→DateTime all handled by Microsoft.Data.Sqlite.
    // SQL Server: UNIQUEIDENTIFIER→string (via ToString), BIT→bool, DATETIME2→DateTime.
    private sealed record SchoolRow(
        string Id, string Name, string Address, string ContactEmail,
        bool IsActive, DateTime RegisteredAt);

    public Task<DrivingSchool?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.Schools.AsNoTracking().FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

    // Hot read path: raw SQL via Dapper avoids EF change-tracker overhead on a read-only list.
    // SQLite stores GUIDs as TEXT — read as string and parse manually (works for SQL Server too:
    // Dapper calls .ToString() on uniqueidentifier before mapping to string).
    public async Task<IReadOnlyList<DrivingSchool>> GetAllActiveAsync(CancellationToken cancellationToken = default)
    {
        var conn = dbContext.Database.GetDbConnection();
        if (conn.State != System.Data.ConnectionState.Open)
            await conn.OpenAsync(cancellationToken);

        // Table name is schema-qualified on SQL Server; SQLite ignores schemas.
        var isSqlite = dbContext.Database.ProviderName?.Contains("Sqlite", StringComparison.OrdinalIgnoreCase) == true;
        var table    = isSqlite ? "Schools" : "schools.Schools";
        var sql = $"""
            SELECT Id, Name, Address, ContactEmail, IsActive, RegisteredAt
            FROM {table}
            WHERE IsActive = 1
            """;

        var rows = await conn.QueryAsync<SchoolRow>(sql);
        return rows
            .Select(r => DrivingSchool.Reconstruct(
                Guid.Parse(r.Id), r.Name, r.Address, r.ContactEmail,
                r.IsActive, r.RegisteredAt))
            .ToList();
    }

    public async Task AddAsync(DrivingSchool school, CancellationToken cancellationToken = default)
    {
        await dbContext.Schools.AddAsync(school, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}

public sealed class InstructorRepository(SchoolsDbContext dbContext) : IInstructorRepository
{
    public Task<Instructor?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.Instructors.AsNoTracking().FirstOrDefaultAsync(i => i.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Instructor>> GetAvailableBySchoolAsync(
        Guid schoolId, CancellationToken cancellationToken = default) =>
        await dbContext.Instructors
            .AsNoTracking()
            .Where(i => i.SchoolId == schoolId && i.IsAvailable)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(Instructor instructor, CancellationToken cancellationToken = default)
    {
        await dbContext.Instructors.AddAsync(instructor, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Instructor instructor, CancellationToken cancellationToken = default)
    {
        dbContext.Instructors.Update(instructor);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
