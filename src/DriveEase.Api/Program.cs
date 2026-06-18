using Azure.Monitor.OpenTelemetry.AspNetCore;
using DriveEase.Api.Messaging;
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
using Microsoft.Extensions.Hosting;
using Microsoft.Identity.Web;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<HostOptions>(opts =>
    opts.BackgroundServiceExceptionBehavior = BackgroundServiceExceptionBehavior.Ignore);

// ── Entra ID authentication ───────────────────────────────────────────────────
// Protects all [Authorize] endpoints with Azure AD JWT bearer tokens.
// TenantId and ClientId are non-secret config values — safe in app settings.
builder.Services.AddMicrosoftIdentityWebApiAuthentication(builder.Configuration, "AzureAd");

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ── Observability (OpenTelemetry → Azure App Insights) ────────────────────────
// Connection string is injected by App Service from its app settings
// (APPLICATIONINSIGHTS_CONNECTION_STRING), which maps to Key Vault in prod.
// Local dev: pipeline active but no exporter — spans are recorded in-process.
var appInsightsConnStr = builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"]
    ?? builder.Configuration["ApplicationInsights:ConnectionString"];

var appInsightsResolved = !string.IsNullOrWhiteSpace(appInsightsConnStr)
    && !appInsightsConnStr.StartsWith("@Microsoft.KeyVault", StringComparison.OrdinalIgnoreCase);

// WithTracing: add our custom source + SqlClient.
// ASP.NET Core and HttpClient tracing are added by UseAzureMonitor (prod) or
// explicitly below (local dev where UseAzureMonitor is not called).
// WithMetrics: register our custom meter only — ASP.NET Core / HttpClient metrics
// come from UseAzureMonitor when present.
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
// Azure: DefaultConnection is a Key Vault reference resolved by the App Service MI.
//        The connection string uses "Authentication=Active Directory Managed Identity"
//        — no password anywhere.
// Local: fall back to per-module SQLite files (no Azure credentials needed).

var sqlConn = builder.Configuration.GetConnectionString("DefaultConnection");

// Guard against unresolved Key Vault references (KV reference not yet propagated).
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
// Azure: ServiceBus__FullyQualifiedNamespace is a Key Vault reference resolved by
//        the App Service MI. AzureServiceBusEventBus uses DefaultAzureCredential —
//        no SAS key anywhere.
// Local: in-process dispatch via InMemoryEventBus.

var sbNamespace = builder.Configuration["ServiceBus:FullyQualifiedNamespace"];

// Guard against unresolved Key Vault references (KV reference not yet propagated).
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

var app = builder.Build();

// Ensure schema exists on cold start.
// SQLite: EnsureCreated is correct (creates DB + tables atomically).
// SQL Server: EnsureCreated skips CreateTables when HasTables() returns true,
//             which breaks on partial schema (some modules created, others not).
//             Fix: execute idempotent IF-NOT-EXISTS SQL directly on the connection.
try
{
    using var scope = app.Services.CreateScope();
    var sp = scope.ServiceProvider;

    if (sqlResolved)
    {
        // SQL Server path — idempotent per-table creation handles partial state
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
            """;
        cmd.ExecuteNonQuery();
        conn.Close();
    }
    else
    {
        // SQLite path — EnsureCreated is safe (creates DB + tables atomically)
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
app.UseSwaggerUI();

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();
