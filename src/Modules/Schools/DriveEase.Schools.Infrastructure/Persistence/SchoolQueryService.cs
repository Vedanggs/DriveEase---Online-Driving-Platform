using Dapper;
using DriveEase.Schools.Application.Queries.GetAllSchools;
using Microsoft.EntityFrameworkCore;

namespace DriveEase.Schools.Infrastructure.Persistence;

// Hot read path optimisation (Day 31): Dapper projects directly to SchoolSummaryDto,
// eliminating the intermediate DrivingSchool.Reconstruct() allocation (51 objects saved per cache miss).
public sealed class SchoolQueryService(SchoolsDbContext dbContext) : ISchoolQueryService
{
    // Use string Id so Dapper works with both SQLite (TEXT) and SQL Server (uniqueidentifier → string).
    private sealed record SchoolSummaryRow(string Id, string Name, string Address, string ContactEmail);

    public async Task<IReadOnlyList<SchoolSummaryDto>> GetAllActiveSummariesAsync(
        CancellationToken cancellationToken = default)
    {
        var conn = dbContext.Database.GetDbConnection();
        if (conn.State != System.Data.ConnectionState.Open)
            await conn.OpenAsync(cancellationToken);

        var isSqlite = dbContext.Database.ProviderName?.Contains("Sqlite", StringComparison.OrdinalIgnoreCase) == true;
        var table    = isSqlite ? "Schools" : "schools.Schools";

        // Cast Id to varchar on SQL Server so Dapper maps it to string regardless of provider.
        var idCol = isSqlite ? "Id" : "CAST(Id AS varchar(36)) AS Id";
        var sql = $"""
            SELECT {idCol}, Name, Address, ContactEmail
            FROM {table}
            WHERE IsActive = 1
            """;

        var rows = await conn.QueryAsync<SchoolSummaryRow>(sql);
        return rows.Select(r => new SchoolSummaryDto(Guid.Parse(r.Id), r.Name, r.Address, r.ContactEmail))
                   .ToList();
    }
}
