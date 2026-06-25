using MediatR;
using Microsoft.Extensions.Caching.Hybrid;

namespace DriveEase.Schools.Application.Queries.GetAllSchools;

public sealed class GetAllSchoolsHandler(
    ISchoolQueryService queryService,
    HybridCache cache)
    : IRequestHandler<GetAllSchoolsQuery, IReadOnlyList<SchoolSummaryDto>>
{
    // Cache key is stable — list changes only when a school is registered or deactivated.
    private const string CacheKey = "schools:all-active";

    public async Task<IReadOnlyList<SchoolSummaryDto>> Handle(
        GetAllSchoolsQuery request, CancellationToken cancellationToken) =>
        await cache.GetOrCreateAsync(
            CacheKey,
            // ISchoolQueryService projects directly to DTO — no domain entity materialization.
            async ct => await queryService.GetAllActiveSummariesAsync(ct),
            new HybridCacheEntryOptions
            {
                Expiration = TimeSpan.FromMinutes(5),
                LocalCacheExpiration = TimeSpan.FromMinutes(1)
            },
            cancellationToken: cancellationToken);
}
