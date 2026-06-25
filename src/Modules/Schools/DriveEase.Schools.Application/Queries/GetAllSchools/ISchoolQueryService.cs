namespace DriveEase.Schools.Application.Queries.GetAllSchools;

// Read-side query service: returns DTOs directly from Dapper, bypassing domain entity construction.
// Registered and implemented in the Infrastructure layer; Application layer owns the interface contract.
public interface ISchoolQueryService
{
    Task<IReadOnlyList<SchoolSummaryDto>> GetAllActiveSummariesAsync(CancellationToken cancellationToken = default);
}
