using Asp.Versioning;
using Azure.Monitor.OpenTelemetry.AspNetCore;
using DriveEase.Api.Messaging;
using DriveEase.Api.Workers;
using Microsoft.EntityFrameworkCore;
using DriveEase.Enrollments.Infrastructure;
using DriveEase.Enrollments.Infrastructure.Persistence;
using DriveEase.Lessons.Infrastructure;
using DriveEase.Lessons.Infrastructure.Persistence;
using DriveEase.Notifications.Infrastructure;
using DriveEase.Schools.Infrastructure;
using DriveEase.Schools.Infrastructure.Persistence;
using DriveEase.Shared.Messaging;
using DriveEase.Shared.Telemetry;
using DriveEase.Students.Infrastructure;
using DriveEase.Students.Infrastructure.Persistence;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Hosting;
using Microsoft.Identity.Web;
using Microsoft.OpenApi.Models;
using OpenTelemetry.Trace;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<HostOptions>(opts =>
    opts.BackgroundServiceExceptionBehavior = BackgroundServiceExceptionBehavior.Ignore);

// Limit request body size globally — prevents memory exhaustion from oversized payloads
builder.WebHost.ConfigureKestrel(k => k.Limits.MaxRequestBodySize = 1_048_576); // 1 MiB

// ── Entra ID authentication ───────────────────────────────────────────────────
builder.Services.AddMicrosoftIdentityWebApiAuthentication(builder.Configuration, "AzureAd");

// ── API versioning ────────────────────────────────────────────────────────────
// Routes: api/v{version}/[controller]  e.g. /api/v1/enrollments
// Response headers: api-supported-versions, api-deprecated-versions
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
})
.AddApiExplorer(options =>
{
    options.GroupNameFormat = "'v'VVV";
    options.SubstituteApiVersionInUrl = true;
});

// ── Rate limiting ─────────────────────────────────────────────────────────────
// Global fixed window: 60 requests per minute per client IP. HTTP 429 on excess.
// GlobalLimiter is used (rather than [EnableRateLimiting] endpoint metadata) so
// the limit applies before routing and auth, covering every path including Swagger.
builder.Services.AddRateLimiter(options =>
{
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "anon",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 60,
                Window = TimeSpan.FromMinutes(1),
                // QueueLimit = 0: reject immediately when permits exhausted (no silent queuing)
                QueueLimit = 0,
            }));
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// ── Swagger with bearer auth scheme ──────────────────────────────────────────
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "DriveEase API", Version = "v1" });

    // Swagger UI will prompt for a bearer token and send Authorization: Bearer <token>
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        Description = "Paste your Entra ID access token (without the 'Bearer ' prefix)",
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id   = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// ── Observability (OpenTelemetry → Azure App Insights) ────────────────────────
var appInsightsConnStr = builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"]
    ?? builder.Configuration["ApplicationInsights:ConnectionString"];

var appInsightsResolved = !string.IsNullOrWhiteSpace(appInsightsConnStr)
    && !appInsightsConnStr.StartsWith("@Microsoft.KeyVault", StringComparison.OrdinalIgnoreCase);

var otelBuilder = builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .AddSource(DriveEaseTelemetry.ServiceName)
        .AddAspNetCoreInstrumentation(opts => opts.RecordException = true)
        .AddHttpClientInstrumentation()
        .AddSqlClientInstrumentation(opts =>
        {
            opts.SetDbStatementForText = true;
            opts.RecordException = true;
        }))
    .WithMetrics(metrics => metrics
        .AddMeter(DriveEaseTelemetry.ServiceName));

if (appInsightsResolved)
    otelBuilder.UseAzureMonitor(opts => opts.ConnectionString = appInsightsConnStr!);

// ── Database ──────────────────────────────────────────────────────────────────
var sqlConn = builder.Configuration.GetConnectionString("DefaultConnection");

var sqlResolved = !string.IsNullOrWhiteSpace(sqlConn) &&
                  !sqlConn.StartsWith("@Microsoft.KeyVault", StringComparison.OrdinalIgnoreCase);

if (sqlResolved)
{
    builder.Services
        .AddEnrollmentsModule(sqlConn!)
        .AddSchoolsModule(sqlConn!)
        .AddStudentsModule(sqlConn!)
        .AddLessonsModule(sqlConn!);
}
else
{
    static string DbPath(string module) =>
        $"Data Source={Path.Combine(Path.GetTempPath(), $"driveease-{module}.db")}";
    builder.Services
        .AddEnrollmentsModule(DbPath("enrollments"))
        .AddSchoolsModule(DbPath("schools"))
        .AddStudentsModule(DbPath("students"))
        .AddLessonsModule(DbPath("lessons"));
}

// ── Event bus ─────────────────────────────────────────────────────────────────
var sbNamespace = builder.Configuration["ServiceBus:FullyQualifiedNamespace"];

var sbResolved = !string.IsNullOrWhiteSpace(sbNamespace) &&
                 !sbNamespace.StartsWith("@Microsoft.KeyVault", StringComparison.OrdinalIgnoreCase);

if (sbResolved)
    builder.Services.AddSingleton<IEventBus>(new AzureServiceBusEventBus(sbNamespace!));
else
    builder.Services.AddSingleton<IEventBus, InMemoryEventBus>();

// ── MediatR ───────────────────────────────────────────────────────────────────
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(
        typeof(DriveEase.Enrollments.Application.Commands.EnrollStudent.EnrollStudentHandler).Assembly);
    cfg.RegisterServicesFromAssembly(
        typeof(DriveEase.Schools.Application.Commands.RegisterSchool.RegisterSchoolHandler).Assembly);
    cfg.RegisterServicesFromAssembly(
        typeof(DriveEase.Students.Application.Commands.RegisterStudent.RegisterStudentHandler).Assembly);
    cfg.RegisterServicesFromAssembly(
        typeof(DriveEase.Lessons.Application.Commands.BookLesson.BookLessonHandler).Assembly);
});

builder.Services.AddNotificationsModule();
builder.Services.AddHostedService<OutboxRelayWorker>();

var app = builder.Build();

// ── Security headers ──────────────────────────────────────────────────────────
// Applied before any other middleware so every response carries the headers.
app.Use(async (ctx, next) =>
{
    ctx.Response.Headers["X-Content-Type-Options"] = "nosniff";
    ctx.Response.Headers["X-Frame-Options"] = "DENY";
    ctx.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    await next();
});

// Rate limiter placed here (before Swagger/auth) so the global limit covers every path
app.UseRateLimiter();

// ── JWT structural pre-check (Span<T>, zero allocation) ──────────────────────
// Rejects Authorization: Bearer headers that are not structurally valid JWTs
// (exactly 3 dot-separated segments) before the request reaches auth middleware.
// Uses ReadOnlySpan<char> to avoid string allocations on every request.
app.Use(async (ctx, next) =>
{
    var raw = ctx.Request.Headers.Authorization.ToString();
    if (raw.Length > 7)
    {
        ReadOnlySpan<char> header = raw.AsSpan();
        if (header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            ReadOnlySpan<char> token = header.Slice(7);
            int dots = 0;
            foreach (char c in token)
                if (c == '.') dots++;
            if (dots != 2)
            {
                ctx.Response.StatusCode = StatusCodes.Status400BadRequest;
                return;
            }
        }
    }
    await next();
});

// ── Schema init on cold start ─────────────────────────────────────────────────
try
{
    using var scope = app.Services.CreateScope();
    var sp = scope.ServiceProvider;

    if (sqlResolved)
    {
        var ctx = sp.GetRequiredService<SchoolsDbContext>();
        var conn = ctx.Database.GetDbConnection();
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = 'schools')    EXEC('CREATE SCHEMA schools');
            IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = 'students')   EXEC('CREATE SCHEMA students');
            IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = 'enrollments') EXEC('CREATE SCHEMA enrollments');
            IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = 'lessons')    EXEC('CREATE SCHEMA lessons');

            IF OBJECT_ID('schools.Schools','U') IS NULL
            BEGIN
                CREATE TABLE schools.Schools (
                    Id           UNIQUEIDENTIFIER NOT NULL,
                    Name         NVARCHAR(200)    NOT NULL,
                    Address      NVARCHAR(500)    NOT NULL,
                    ContactEmail NVARCHAR(200)    NOT NULL,
                    IsActive     BIT              NOT NULL,
                    RegisteredAt DATETIME2        NOT NULL,
                    CONSTRAINT PK_Schools PRIMARY KEY (Id));
            END

            IF OBJECT_ID('schools.Instructors','U') IS NULL
            BEGIN
                CREATE TABLE schools.Instructors (
                    Id            UNIQUEIDENTIFIER NOT NULL,
                    SchoolId      UNIQUEIDENTIFIER NOT NULL,
                    FullName      NVARCHAR(200)    NOT NULL,
                    LicenseNumber NVARCHAR(50)     NOT NULL,
                    IsAvailable   BIT              NOT NULL,
                    CONSTRAINT PK_Instructors PRIMARY KEY (Id));
                CREATE INDEX IX_Instructors_SchoolId ON schools.Instructors (SchoolId);
            END

            IF OBJECT_ID('students.Students','U') IS NULL
            BEGIN
                CREATE TABLE students.Students (
                    Id           UNIQUEIDENTIFIER NOT NULL,
                    FullName     NVARCHAR(200)    NOT NULL,
                    Email        NVARCHAR(200)    NOT NULL,
                    PhoneNumber  NVARCHAR(30)     NULL,
                    DateOfBirth  DATE             NOT NULL,
                    RegisteredAt DATETIME2        NOT NULL,
                    PasswordHash NVARCHAR(500)    NOT NULL,
                    CONSTRAINT PK_Students PRIMARY KEY (Id));
                CREATE UNIQUE INDEX IX_Students_Email ON students.Students (Email);
            END

            IF OBJECT_ID('enrollments.Enrollments','U') IS NULL
            BEGIN
                CREATE TABLE enrollments.Enrollments (
                    Id                 UNIQUEIDENTIFIER NOT NULL,
                    StudentId          UNIQUEIDENTIFIER NOT NULL,
                    DrivingSchoolId    UNIQUEIDENTIFIER NOT NULL,
                    InstructorId       UNIQUEIDENTIFIER NULL,
                    Fee                DECIMAL(18,2)    NOT NULL,
                    PaymentStatus      NVARCHAR(MAX)    NOT NULL,
                    Status             NVARCHAR(MAX)    NOT NULL,
                    EnrolledAt         DATETIME2        NOT NULL,
                    PaymentConfirmedAt DATETIME2        NULL,
                    CancelledAt        DATETIME2        NULL,
                    CONSTRAINT PK_Enrollments PRIMARY KEY (Id));
                CREATE INDEX IX_Enrollments_StudentId ON enrollments.Enrollments (StudentId);
                CREATE INDEX IX_Enrollments_StudentId_Status ON enrollments.Enrollments (StudentId, Status);
            END

            IF OBJECT_ID('lessons.Lessons','U') IS NULL
            BEGIN
                CREATE TABLE lessons.Lessons (
                    Id           UNIQUEIDENTIFIER NOT NULL,
                    EnrollmentId UNIQUEIDENTIFIER NOT NULL,
                    StudentId    UNIQUEIDENTIFIER NOT NULL,
                    InstructorId UNIQUEIDENTIFIER NOT NULL,
                    ScheduledAt  DATETIME2        NOT NULL,
                    Duration     FLOAT            NOT NULL,
                    Status       NVARCHAR(MAX)    NOT NULL,
                    Notes        NVARCHAR(MAX)    NULL,
                    CompletedAt  DATETIME2        NULL,
                    CONSTRAINT PK_Lessons PRIMARY KEY (Id));
                CREATE INDEX IX_Lessons_StudentId_ScheduledAt ON lessons.Lessons (StudentId, ScheduledAt);
                CREATE INDEX IX_Lessons_EnrollmentId ON lessons.Lessons (EnrollmentId);
            END

            IF OBJECT_ID('enrollments.OutboxMessages','U') IS NULL
            BEGIN
                CREATE TABLE enrollments.OutboxMessages (
                    Id          UNIQUEIDENTIFIER NOT NULL,
                    EventType   NVARCHAR(500)    NOT NULL,
                    Payload     NVARCHAR(MAX)    NOT NULL,
                    CreatedAt   DATETIME2        NOT NULL,
                    ProcessedAt DATETIME2        NULL,
                    Error       NVARCHAR(MAX)    NULL,
                    CONSTRAINT PK_EnrollmentOutboxMessages PRIMARY KEY (Id));
                CREATE INDEX IX_EnrollmentOutbox_Unprocessed ON enrollments.OutboxMessages (ProcessedAt)
                    WHERE ProcessedAt IS NULL;
            END

            IF OBJECT_ID('lessons.OutboxMessages','U') IS NULL
            BEGIN
                CREATE TABLE lessons.OutboxMessages (
                    Id          UNIQUEIDENTIFIER NOT NULL,
                    EventType   NVARCHAR(500)    NOT NULL,
                    Payload     NVARCHAR(MAX)    NOT NULL,
                    CreatedAt   DATETIME2        NOT NULL,
                    ProcessedAt DATETIME2        NULL,
                    Error       NVARCHAR(MAX)    NULL,
                    CONSTRAINT PK_LessonOutboxMessages PRIMARY KEY (Id));
                CREATE INDEX IX_LessonOutbox_Unprocessed ON lessons.OutboxMessages (ProcessedAt)
                    WHERE ProcessedAt IS NULL;
            END
            """;
        cmd.ExecuteNonQuery();
        conn.Close();
    }
    else
    {
        sp.GetRequiredService<EnrollmentsDbContext>().Database.EnsureCreated();
        sp.GetRequiredService<StudentsDbContext>().Database.EnsureCreated();
        sp.GetRequiredService<SchoolsDbContext>().Database.EnsureCreated();
        sp.GetRequiredService<LessonsDbContext>().Database.EnsureCreated();
    }
}
catch (Exception ex)
{
    var startupLogger = app.Services.GetRequiredService<ILogger<Program>>();
    startupLogger.LogWarning(ex, "Schema init failed on startup — app will start but SQL operations may fail until the issue resolves.");
}

app.UseSwagger();
app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "DriveEase API v1"));

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();
