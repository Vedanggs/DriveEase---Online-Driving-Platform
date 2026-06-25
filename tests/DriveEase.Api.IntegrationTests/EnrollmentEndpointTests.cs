using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace DriveEase.Api.IntegrationTests;

/// <summary>
/// Focused tests for POST/GET /api/v1/enrollments:
///   - 401 without token
///   - 400 on Fee validation (zero, negative, over max)
///   - 201 happy-path enroll
///   - 404 for unknown enrollment ID
///   - 200 for GET /me with a valid student token
/// </summary>
public sealed class EnrollmentEndpointTests(DriveEaseWebApplicationFactory factory, ITestOutputHelper output)
    : IClassFixture<DriveEaseWebApplicationFactory>
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private readonly HttpClient _anon = factory.CreateClient();

    // ── 401 without token ─────────────────────────────────────────────────────

    [Fact]
    public async Task PostEnroll_WithoutToken_Returns401()
    {
        var body = Serialize(new { StudentId = Guid.NewGuid(), DrivingSchoolId = Guid.NewGuid(), Fee = 500 });
        var response = await _anon.PostAsync("/api/v1/enrollments", body);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetEnrollmentById_WithoutToken_Returns401()
    {
        var response = await _anon.GetAsync($"/api/v1/enrollments/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── Validation: Fee range ─────────────────────────────────────────────────

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-999.99)]
    public async Task PostEnroll_WithFeeAtOrBelowZero_Returns400(decimal fee)
    {
        var client = factory.CreateStudentClient(Guid.NewGuid());
        var body   = Serialize(new { StudentId = Guid.NewGuid(), DrivingSchoolId = Guid.NewGuid(), Fee = fee });

        var response = await client.PostAsync("/api/v1/enrollments", body);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest,
            $"Fee={fee} violates [Range(1.0, 100_000.0)] on EnrollStudentCommand");
    }

    [Fact]
    public async Task PostEnroll_WithFeeExceedingMaximum_Returns400()
    {
        var client = factory.CreateStudentClient(Guid.NewGuid());
        var body   = Serialize(new { StudentId = Guid.NewGuid(), DrivingSchoolId = Guid.NewGuid(), Fee = 200_000 });

        var response = await client.PostAsync("/api/v1/enrollments", body);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest,
            "Fee=200_000 exceeds [Range(1.0, 100_000.0)] max");
    }

    // ── Happy path ────────────────────────────────────────────────────────────

    [Fact]
    public async Task PostEnroll_WithValidStudentAndSchool_Returns201WithId()
    {
        // Register a real student so the handler can find them in the DB.
        var email = $"{Guid.NewGuid():N}@enroll.test";
        var regResp = await _anon.PostAsync("/api/v1/auth/register", Serialize(new
        {
            FullName    = "Enroll Happy",
            Email       = email,
            Password    = "Enroll@1234",
            DateOfBirth = "2000-06-01",
        }));
        regResp.EnsureSuccessStatusCode();
        var regDoc   = JsonDocument.Parse(await regResp.Content.ReadAsStringAsync());
        var studentId = regDoc.RootElement.GetProperty("id").GetGuid();

        // Login to get a real JWT issued by the app.
        var loginResp = await _anon.PostAsync("/api/v1/auth/login",
            Serialize(new { Email = email, Password = "Enroll@1234" }));
        loginResp.EnsureSuccessStatusCode();
        var loginDoc  = JsonDocument.Parse(await loginResp.Content.ReadAsStringAsync());
        var token     = loginDoc.RootElement.GetProperty("accessToken").GetString()!;

        var authClient = factory.CreateClient();
        authClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        // Pick the first seeded school.
        var schoolsDoc = JsonDocument.Parse(
            await (await _anon.GetAsync("/api/v1/schools")).Content.ReadAsStringAsync());
        var schoolId = Guid.Parse(schoolsDoc.RootElement[0].GetProperty("id").GetString()!);

        // Enroll
        var enrollResp = await authClient.PostAsync("/api/v1/enrollments",
            Serialize(new { StudentId = studentId, DrivingSchoolId = schoolId, Fee = 750 }));

        enrollResp.StatusCode.Should().Be(HttpStatusCode.Created,
            "a valid student+school+fee should create an enrollment");

        var enrollDoc = JsonDocument.Parse(await enrollResp.Content.ReadAsStringAsync());
        enrollDoc.RootElement.TryGetProperty("id", out _).Should().BeTrue();
        output.WriteLine($"Enrollment created: studentId={studentId} schoolId={schoolId}");
    }

    // ── 404 and GET /me ───────────────────────────────────────────────────────

    [Fact]
    public async Task GetEnrollmentById_WithUnknownId_Returns404()
    {
        var client   = factory.CreateStudentClient(Guid.NewGuid());
        var response = await client.GetAsync($"/api/v1/enrollments/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetMyEnrollment_WithValidStudentToken_ReturnsSuccessStatus()
    {
        // Synthetic JWT — student has no enrollment yet.
        // Ok(null) in ASP.NET Core 9+ emits 204 No Content when there is no body.
        var client   = factory.CreateStudentClient(Guid.NewGuid());
        var response = await client.GetAsync("/api/v1/enrollments/me");
        response.IsSuccessStatusCode.Should().BeTrue(
            "authenticated GET /me should succeed (200 or 204) regardless of whether the student has an enrollment");
    }

    // ── Helper ────────────────────────────────────────────────────────────────

    private static StringContent Serialize(object body) =>
        new(JsonSerializer.Serialize(body, Json), Encoding.UTF8, "application/json");
}
