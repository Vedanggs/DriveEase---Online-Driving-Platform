using System.Net;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace DriveEase.Api.IntegrationTests;

[Collection("IntegrationTests")]
public sealed class SecurityTests(DriveEaseWebApplicationFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient();

    // ── Security headers ─────────────────────────────────────────────────────

    [Theory]
    [InlineData("X-Content-Type-Options", "nosniff")]
    [InlineData("X-Frame-Options", "DENY")]
    [InlineData("Referrer-Policy", "strict-origin-when-cross-origin")]
    public async Task AllResponses_ContainSecurityHeader(string header, string expectedValue)
    {
        var response = await _client.GetAsync("/api/v1/schools");

        response.Headers.TryGetValues(header, out var values).Should().BeTrue(
            $"response should include {header}");
        values!.First().Should().Be(expectedValue);
    }

    // ── Authentication ───────────────────────────────────────────────────────

    [Fact]
    public async Task ProtectedEndpoint_WithoutToken_Returns401()
    {
        // POST /api/v1/enrollments requires auth
        var body = new StringContent(
            JsonSerializer.Serialize(new { StudentId = Guid.NewGuid(), DrivingSchoolId = Guid.NewGuid(), Fee = 500 }),
            Encoding.UTF8, "application/json");

        var response = await _client.PostAsync("/api/v1/enrollments", body);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task MalformedJwt_Returns400()
    {
        // JWT pre-check in Program.cs rejects tokens that don't have exactly 2 dots
        _client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", "Bearer not.a.valid.jwt.token");

        var response = await _client.GetAsync("/api/v1/schools");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest,
            "zero-allocation JWT pre-check rejects tokens with wrong dot count before auth handler runs");

        // Clean up for other tests in the same client
        _client.DefaultRequestHeaders.Remove("Authorization");
    }

    [Fact]
    public async Task AnonymousEndpoint_WithoutToken_Returns200()
    {
        // GET /api/v1/schools is AllowAnonymous
        var response = await _client.GetAsync("/api/v1/schools");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ── Request validation ───────────────────────────────────────────────────

    [Fact]
    public async Task InvalidJsonBody_Returns400()
    {
        // Malformed JSON body on a POST that deserializes a command → 400 Bad Request
        var body = new StringContent("{ this is not valid json }", Encoding.UTF8, "application/json");

        var response = await _client.PostAsync("/api/v1/auth/register", body);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // Note: Kestrel MaxRequestBodySize = 1 MiB is enforced by Kestrel, not the TestServer.
    // The 413 behaviour is validated in load/smoke tests against the real server.

    // ── Health check ─────────────────────────────────────────────────────────

    [Fact]
    public async Task HealthCheck_Returns200()
    {
        var response = await _client.GetAsync("/health");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
