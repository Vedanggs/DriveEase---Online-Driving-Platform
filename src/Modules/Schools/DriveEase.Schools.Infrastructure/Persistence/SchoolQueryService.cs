using Dapper;
using DriveEase.Schools.Application.Queries.GetAllSchools;
using Microsoft.EntityFrameworkCore;

namespace DriveEase.Schools.Infrastructure.Persistence;

// Hot read path optimisation (Day 31): Dapper projects directly to SchoolSummaryDto,
// eliminating the intermediate DrivingSchool.Reconstruct() allocation (51 objects saved per cache miss).
public sealed class SchoolQueryService(SchoolsDbContext dbContext) : ISchoolQueryService
{
    // SQLite stores GUIDs as TEXT; SQL Server returns uniqueidentifier.
    // Dapper maps both to string before we parse — works for both providers.
    private sealed record SchoolSummaryRow(string Id, string Name, string Address, string ContactEmail);

    public async Task<IReadOnlyList<SchoolSummaryDto>> GetAllActiveSummariesAsync(
        CancellationToken cancellationToken = default)
    {
        // EF Core owns the connection lifecycle (closed when DbContext is disposed at end of DI scope).
        var conn = dbContext.Database.GetDbConnection();
        if (conn.State != System.Data.ConnectionState.Open)
            await conn.OpenAsync(cancellationToken);

        // Table name is schema-qualified on SQL Server; SQLite ignores schemas.
        var isSqlite = dbContext.Database.ProviderName?.Contains("Sqlite", StringComparison.OrdinalIgnoreCase) == true;
        var table    = isSqlite ? "Schools" : "schools.Schools";
        var sql = $"""
            SELECT Id, Name, Address, ContactEmail
            FROM {table}
            WHERE IsActive = 1
            """;

        var rows = await conn.QueryAsync<SchoolSummaryRow>(sql);
        return rows.Select(r => new SchoolSummaryDto(Guid.Parse(r.Id), r.Name, r.Address, r.ContactEmail))
                   .ToList();
    }
}
