using System.Diagnostics;

namespace DriveEase.Shared.Telemetry;

// Injects the current W3C TraceContext (traceparent / tracestate) into any
// string-keyed property bag (Service Bus ApplicationProperties, HTTP headers, etc.).
// Keeping this in Shared means it can be tested with a plain Dictionary — no Azure SDK needed.
public static class TraceContextPropagator
{
    public static void Inject(IDictionary<string, object> properties)
    {
        if (Activity.Current is not { } current) return;

        properties["traceparent"] = current.Id!;

        if (!string.IsNullOrEmpty(current.TraceStateString))
            properties["tracestate"] = current.TraceStateString;
    }
}
