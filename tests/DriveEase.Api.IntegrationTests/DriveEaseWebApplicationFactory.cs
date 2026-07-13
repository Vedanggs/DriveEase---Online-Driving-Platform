using System.Net.Http.Headers;
using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Testcontainers.MsSql;
using Xunit;

namespace DriveEase.Api.IntegrationTests;

/// <summary>
/// Integration test host backed by a real SQL Server container via Testcontainers + Docker Desktop.
///
/// Lifecycle (managed by xUnit IClassFixture):
///   1. InitializeAsync  — Docker Desktop spins up mcr.microsoft.com/mssql/server:2022-latest,
///                         waits for the health-check, then returns the connection string.
///   2. ConfigureWebHost — connection string injected into config; each module's AddDbContext
///                         sees a non-SQLite prefix and auto-selects UseSqlServer.
///                         UseEnsureCreated=true makes Program.cs call EnsureCreated() instead
///                         of MigrateAsync() so EF generates SQL Server schema from the model
///                         (the stored migration files are SQLite-only and cannot run on MSSQL).
///   3. Tests run        — against a real SQL Server instance with seeded data.
///   4. DisposeAsync     — shuts down the test host, then docker stop + docker rm the container.
/// </summary>
public sealed class DriveEaseWebApplicationFactory
    : WebApplicationFactory<Program>, IAsyncLifetime
{
    // Built and started in InitializeAsync() — deferred so the constructor never touches Docker.
    // Testcontainers issues docker run, waits for the SQL Server health-check, then StartAsync() returns.
    private MsSqlContainer? _sql;

    // Set in InitializeAsync(); read inside ConfigureWebHost().
    // xUnit guarantees InitializeAsync() completes before any CreateClient() call.
    private string _connectionString = string.Empty;

    public const string TestJwtKey      = "driveease-test-secret-key-32chars!!";
    public const string TestJwtIssuer   = "test-issuer";
    public const string TestJwtAudience = "test-audience";

    // ── IAsyncLifetime ────────────────────────────────────────────────────────

    public async Task InitializeAsync()
    {
        // Build() here (not in the field initializer) so the constructor never touches Docker.
        // If Docker Desktop is not running, the error surfaces in InitializeAsync(), not the ctor.
        _sql = new MsSqlBuilder()
            .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
            .Build();
        await _sql.StartAsync();
        _connectionString = _sql.GetConnectionString();
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await base.DisposeAsync();                 // shut down ASP.NET test host
        if (_sql is not null) await _sql.DisposeAsync();  // docker stop + docker rm
    }

    // ── WebApplicationFactory ─────────────────────────────────────────────────

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                // SQL Server connection string from the running container.
                // Each module's AddDbContext checks for "Data Source=" to pick SQLite;
                // a "Server=..." string automatically routes to UseSqlServer instead.
                ["ConnectionStrings:DefaultConnection"] = _connectionString,
                // Bypass the SQLite-specific migration files. EnsureCreated() generates
                // a proper SQL Server schema directly from the EF Core model.
                ["UseEnsureCreated"]  = "true",
                ["Jwt:Key"]           = TestJwtKey,
                ["Jwt:Issuer"]        = TestJwtIssuer,
                ["Jwt:Audience"]      = TestJwtAudience,
                ["Jwt:ExpiryMinutes"] = "60",
                ["AzureAd:TenantId"] = "00000000-0000-0000-0000-000000000000",
                ["AzureAd:ClientId"] = "00000000-0000-0000-0000-000000000000",
                ["AzureAd:Instance"] = "https://login.microsoftonline.com/",
                ["APPLICATIONINSIGHTS_CONNECTION_STRING"] = "",
                ["ServiceBus:FullyQualifiedNamespace"]    = "",
                ["Workers:LessonReminderIntervalSeconds"] = "86400",
            });
        });

        builder.ConfigureTestServices(services =>
        {
            // Bearer (Azure AD): disable authority validation entirely.
            // .NET 8+ JsonWebTokenHandler requires SignatureValidator to return JsonWebToken.
            services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, opts =>
            {
                opts.Authority = null;
                opts.ConfigurationManager = null;
                opts.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer           = false,
                    ValidateAudience         = false,
                    ValidateLifetime         = false,
                    ValidateIssuerSigningKey = false,
                    RequireSignedTokens      = false,
                    SignatureValidator = (token, _) => new JsonWebTokenHandler().ReadJsonWebToken(token),
                };
            });

            // Disable global rate limiter so rapid-fire test loops don't hit 429.
            services.PostConfigure<RateLimiterOptions>(opts =>
            {
                opts.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(
                    _ => RateLimitPartition.GetNoLimiter("test"));
            });

            // StudentBearer: configure with the test key directly (belt-and-suspenders).
            services.PostConfigure<JwtBearerOptions>("StudentBearer", opts =>
            {
                opts.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer           = true,
                    ValidIssuer              = TestJwtIssuer,
                    ValidateAudience         = true,
                    ValidAudience            = TestJwtAudience,
                    ValidateLifetime         = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestJwtKey)),
                };
            });
        });
    }

    /// <summary>Creates an HttpClient pre-loaded with a StudentBearer JWT for the given student ID.</summary>
    public HttpClient CreateStudentClient(Guid studentId, string email = "student@test.com", string fullName = "Test Student")
    {
        var token = JwtTestHelper.GenerateStudentToken(
            studentId, email, fullName, TestJwtKey, TestJwtIssuer, TestJwtAudience);
        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    /// <summary>Creates an HttpClient pre-loaded with an Instructor JWT for the given instructor ID.</summary>
    public HttpClient CreateInstructorClient(Guid instructorId, string email = "instructor@test.com", string fullName = "Test Instructor")
    {
        var token = JwtTestHelper.GenerateInstructorToken(
            instructorId, email, fullName, TestJwtKey, TestJwtIssuer, TestJwtAudience);
        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }
}
