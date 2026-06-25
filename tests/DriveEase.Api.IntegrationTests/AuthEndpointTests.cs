using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace DriveEase.Api.IntegrationTests;

/// <summary>
/// Tests for POST /api/v1/auth/{register,login,logout}:
///   - Register: happy path, email validation, short password, missing name
///   - Login: happy path, wrong password, non-existent user
///   - Role-based access: 403 when Student policy is required but role claim is absent
/// </summary>
public sealed class AuthEndpointTests(DriveEaseWebApplicationFactory factory)
    : IClassFixture<DriveEaseWebApplicationFactory>
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private readonly HttpClient _client = factory.CreateClient();

    // ── Register ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Register_WithValidData_Returns200WithId()
    {
        var email    = $"{Guid.NewGuid():N}@auth.test";
        var response = await _client.PostAsync("/api/v1/auth/register", Serialize(new
        {
            FullName    = "Auth Happy",
            Email       = email,
            Password    = "ValidPass1!",
            DateOfBirth = "1995-06-15",
        }));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        doc.RootElement.TryGetProperty("id", out _).Should().BeTrue("register should return the new student id");
    }

    [Fact]
    public async Task Register_WithInvalidEmail_Returns400()
    {
        var response = await _client.PostAsync("/api/v1/auth/register", Serialize(new
        {
            FullName    = "Bad Email",
            Email       = "not-an-email-address",
            Password    = "ValidPass1!",
            DateOfBirth = "1995-06-15",
        }));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest,
            "[EmailAddress] on RegisterStudentCommand.Email should reject malformed emails");
    }

    [Fact]
    public async Task Register_WithPasswordTooShort_Returns400()
    {
        var response = await _client.PostAsync("/api/v1/auth/register", Serialize(new
        {
            FullName    = "Short Pwd",
            Email       = $"{Guid.NewGuid():N}@auth.test",
            Password    = "abc",   // < 8 chars
            DateOfBirth = "1995-06-15",
        }));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest,
            "[MinLength(8)] on RegisterStudentCommand.Password should reject 3-char passwords");
    }

    [Fact]
    public async Task Register_WithEmptyFullName_Returns400()
    {
        var response = await _client.PostAsync("/api/v1/auth/register", Serialize(new
        {
            FullName    = "",   // violates [Required]
            Email       = $"{Guid.NewGuid():N}@auth.test",
            Password    = "ValidPass1!",
            DateOfBirth = "1995-06-15",
        }));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest,
            "[Required] on RegisterStudentCommand.FullName should reject empty string");
    }

    // ── Login ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Login_WithValidCredentials_Returns200WithBothTokens()
    {
        var email = $"{Guid.NewGuid():N}@login.test";
        await _client.PostAsync("/api/v1/auth/register", Serialize(new
        {
            FullName    = "Login Happy",
            Email       = email,
            Password    = "LoginPass1!",
            DateOfBirth = "1998-03-20",
        }));

        var response = await _client.PostAsync("/api/v1/auth/login",
            Serialize(new { Email = email, Password = "LoginPass1!" }));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        doc.RootElement.GetProperty("accessToken").GetString().Should().NotBeNullOrWhiteSpace();
        doc.RootElement.GetProperty("refreshToken").GetString().Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Login_WithWrongPassword_Returns401()
    {
        var email = $"{Guid.NewGuid():N}@login.test";
        await _client.PostAsync("/api/v1/auth/register", Serialize(new
        {
            FullName    = "Wrong Pwd",
            Email       = email,
            Password    = "CorrectPass1!",
            DateOfBirth = "1998-03-20",
        }));

        var response = await _client.PostAsync("/api/v1/auth/login",
            Serialize(new { Email = email, Password = "WrongPassword123!" }));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_WithNonExistentEmail_Returns401()
    {
        var response = await _client.PostAsync("/api/v1/auth/login",
            Serialize(new { Email = "nobody@doesnotexist.com", Password = "AnyPass1!" }));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── 403: authenticated but wrong role ─────────────────────────────────────

    [Fact]
    public async Task Logout_WithTokenMissingStudentRole_Returns403()
    {
        // Build a JWT signed with the test key so StudentBearer accepts it,
        // but WITHOUT ClaimTypes.Role = "Student".
        // [Authorize(Policy = "Student")] requires StudentBearer scheme + Student role → 403.
        var signingKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(DriveEaseWebApplicationFactory.TestJwtKey));

        var jwtToken = new JwtSecurityToken(
            issuer   : DriveEaseWebApplicationFactory.TestJwtIssuer,
            audience : DriveEaseWebApplicationFactory.TestJwtAudience,
            claims   : new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub,   Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Email, "norole@test.com"),
                // Intentionally NO ClaimTypes.Role = "Student"
            },
            expires          : DateTime.UtcNow.AddHours(1),
            signingCredentials: new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256));

        var tokenString = new JwtSecurityTokenHandler().WriteToken(jwtToken);
        var noRoleClient = factory.CreateClient();
        noRoleClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", tokenString);

        // POST /api/v1/auth/logout carries [Authorize(Policy = "Student")].
        // Authorization runs before the body is read, so the RefreshToken value doesn't matter.
        var response = await noRoleClient.PostAsync("/api/v1/auth/logout",
            Serialize(new { RefreshToken = "not-a-real-token" }));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden,
            "authenticated user lacking the Student role should be forbidden from Student-policy endpoints");
    }

    // ── Helper ────────────────────────────────────────────────────────────────

    private static StringContent Serialize(object body) =>
        new(JsonSerializer.Serialize(body, Json), Encoding.UTF8, "application/json");
}
