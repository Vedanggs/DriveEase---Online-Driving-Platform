using System.Net;
using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace DriveEase.Api.IntegrationTests;

public sealed class SchoolsEndpointTests(DriveEaseWebApplicationFactory factory)
    : IClassFixture<DriveEaseWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task GetAll_ReturnsOkWithSeededSchools()
    {
        var response = await _client.GetAsync("/api/v1/schools");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        doc.RootElement.GetArrayLength().Should().BeGreaterThanOrEqualTo(17,
            "DatabaseSeeder seeds 17 schools on cold start");
    }

    [Fact]
    public async Task GetAll_SchoolsHaveExpectedFields()
    {
        var response = await _client.GetAsync("/api/v1/schools");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        var first = doc.RootElement[0];

        first.TryGetProperty("id", out _).Should().BeTrue();
        first.TryGetProperty("name", out _).Should().BeTrue();
        first.TryGetProperty("address", out _).Should().BeTrue();
        first.TryGetProperty("contactEmail", out _).Should().BeTrue();
    }

    [Fact]
    public async Task GetAll_SubsequentCallsServeCachedResult()
    {
        // Two back-to-back calls — both should succeed (second hits HybridCache L1)
        var r1 = await _client.GetAsync("/api/v1/schools");
        var r2 = await _client.GetAsync("/api/v1/schools");

        r1.StatusCode.Should().Be(HttpStatusCode.OK);
        r2.StatusCode.Should().Be(HttpStatusCode.OK);

        var body1 = await r1.Content.ReadAsStringAsync();
        var body2 = await r2.Content.ReadAsStringAsync();
        body1.Should().Be(body2, "cached response should be identical");
    }

    [Fact]
    public async Task GetInstructors_KnownSchool_ReturnsOkWithInstructors()
    {
        // Fetch the schools list to get a real school ID
        var schoolsResp = await _client.GetAsync("/api/v1/schools");
        var schoolsBody = await schoolsResp.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(schoolsBody);
        var schoolId = doc.RootElement[0].GetProperty("id").GetString();

        var response = await _client.GetAsync($"/api/v1/schools/{schoolId}/instructors");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var instrBody = await response.Content.ReadAsStringAsync();
        using var instrDoc = JsonDocument.Parse(instrBody);
        instrDoc.RootElement.GetArrayLength().Should().Be(3,
            "each seeded school has exactly 3 instructors");
    }

    [Fact]
    public async Task GetInstructors_UnknownSchool_ReturnsOkWithEmptyList()
    {
        var response = await _client.GetAsync($"/api/v1/schools/{Guid.NewGuid()}/instructors");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Be("[]");
    }
}
