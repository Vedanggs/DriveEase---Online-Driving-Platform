using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace DriveEase.Shared.Telemetry;

// Central telemetry definitions — all modules emit to these shared sources.
// ActivitySource and Meter are BCL types; no extra NuGet packages needed here.
public static class DriveEaseTelemetry
{
    public const string ServiceName    = "DriveEase.Api";
    public const string ServiceVersion = "1.0.0";

    // All custom spans (workers, event bus) report under this source.
    // Registered in Program.cs via .AddSource(DriveEaseTelemetry.ServiceName).
    public static readonly ActivitySource Source = new(ServiceName, ServiceVersion);

    // Custom metrics exported to App Insights customMetrics table.
    public static readonly Meter Meter = new(ServiceName, ServiceVersion);

    public static readonly Counter<long> EnrollmentsCreated =
        Meter.CreateCounter<long>("driveease.enrollments.created", "count");

    public static readonly Counter<long> PaymentsProcessed =
        Meter.CreateCounter<long>("driveease.payments.processed", "count");

    public static readonly Counter<long> LessonRemindersSent =
        Meter.CreateCounter<long>("driveease.lessons.reminders_sent", "count");

    public static readonly Counter<long> EnrollmentsAutoCancelled =
        Meter.CreateCounter<long>("driveease.enrollments.auto_cancelled", "count");
}
