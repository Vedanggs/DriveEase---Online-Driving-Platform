using System.Diagnostics;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace DriveEase.Api.IntegrationTests;

/// <summary>
/// Hot-path p99 latency guard for GET /api/v1/schools.
///
/// Strategy:
///   - 1 warmup request fills HybridCache L1 (measures cold / cache-miss path)
///   - 100 measured requests all hit the in-process L1 cache (measures warm / cache-hit path)
///
/// Optimization applied in Day 31:
///   Before — Dapper → SchoolRow → DrivingSchool.Reconstruct() × 51 → SchoolSummaryDto × 51
///           (3 object types per row; 153 allocations per cache miss)
///   After  — Dapper → SchoolSummaryDto × 51 directly via ISchoolQueryService
///           (2 object types per row; 102 allocations per cache miss, –51 DrivingSchool objects)
///
/// The warm-path p99 is invariant to the optimization (cache hit = same after both).
/// The cold-path is faster after the optimization (fewer allocations + no private-ctor reflection).
/// </summary>
public sealed class PerformanceTests(DriveEaseWebApplicationFactory factory, ITestOutputHelper output)
    : IClassFixture<DriveEaseWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task GetAllSchools_WarmPath_P99Under100Ms()
    {
        const int iterations = 100;

        // Warmup: first request populates HybridCache L1; we record this as the cold-path reading.
        var coldSw = Stopwatch.StartNew();
        var warmup = await _client.GetAsync("/api/v1/schools");
        coldSw.Stop();
        warmup.EnsureSuccessStatusCode();
        var coldMs = coldSw.Elapsed.TotalMilliseconds;

        // Measured run: all subsequent requests hit the in-process L1 cache.
        var latencies = new List<double>(iterations);
        for (var i = 0; i < iterations; i++)
        {
            var sw = Stopwatch.StartNew();
            var resp = await _client.GetAsync("/api/v1/schools");
            sw.Stop();
            resp.EnsureSuccessStatusCode();
            latencies.Add(sw.Elapsed.TotalMilliseconds);
        }

        latencies.Sort();

        var p50 = Percentile(latencies, 50);
        var p95 = Percentile(latencies, 95);
        var p99 = Percentile(latencies, 99);
        var max = latencies[^1];

        output.WriteLine($"GET /api/v1/schools ({iterations} warm-path iterations)");
        output.WriteLine($"  Cold (cache miss, post-optimization): {coldMs:F1} ms");
        output.WriteLine($"  p50  : {p50:F1} ms");
        output.WriteLine($"  p95  : {p95:F1} ms");
        output.WriteLine($"  p99  : {p99:F1} ms");
        output.WriteLine($"  max  : {max:F1} ms");

        p99.Should().BeLessThan(100,
            "warm-path p99 should be well under 100 ms for an in-process HybridCache hit");
    }

    private static double Percentile(List<double> sorted, int pct)
    {
        var idx = (int)Math.Ceiling(pct / 100.0 * sorted.Count) - 1;
        return sorted[Math.Clamp(idx, 0, sorted.Count - 1)];
    }
}
