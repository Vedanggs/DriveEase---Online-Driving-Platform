using DriveEase.Api.Messaging;
using DriveEase.Enrollments.Infrastructure;
using DriveEase.Enrollments.Infrastructure.Persistence;
using DriveEase.Lessons.Infrastructure;
using DriveEase.Lessons.Infrastructure.Persistence;
using DriveEase.Notifications.Infrastructure;
using DriveEase.Schools.Infrastructure;
using DriveEase.Schools.Infrastructure.Persistence;
using DriveEase.Shared.Messaging;
using DriveEase.Students.Infrastructure;
using DriveEase.Students.Infrastructure.Persistence;
using Microsoft.Extensions.Hosting;
using Microsoft.Identity.Web;

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

// Ensure schema exists on cold start (EnsureCreated is idempotent)
using (var scope = app.Services.CreateScope())
{
    var sp = scope.ServiceProvider;
    sp.GetRequiredService<EnrollmentsDbContext>().Database.EnsureCreated();
    sp.GetRequiredService<StudentsDbContext>().Database.EnsureCreated();
    sp.GetRequiredService<SchoolsDbContext>().Database.EnsureCreated();
    sp.GetRequiredService<LessonsDbContext>().Database.EnsureCreated();
}

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();
