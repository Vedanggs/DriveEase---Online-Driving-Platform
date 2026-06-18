using System.Diagnostics;
using DriveEase.Shared.Telemetry;
using FluentAssertions;
using Xunit;

namespace DriveEase.Api.Tests;

public sealed class TelemetryDefinitionTests
{
    [Fact]
    public void ActivitySource_HasExpectedServiceName()
    {
        DriveEaseTelemetry.ServiceName.Should().Be("DriveEase.Api");
        DriveEaseTelemetry.Source.Name.Should().Be("DriveEase.Api");
    }

    [Fact]
    public void Meter_HasExpectedName()
    {
        DriveEaseTelemetry.Meter.Name.Should().Be("DriveEase.Api");
    }

    [Fact]
    public void ActivitySource_StartsActivity_WhenListenerIsRegistered()
    {
        Activity? recorded = null;

        using var listener = new ActivityListener
        {
            ShouldListenTo = s => s.Name == DriveEaseTelemetry.ServiceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) =>
                ActivitySamplingResult.AllData,
            ActivityStarted = a => recorded = a
        };
        ActivitySource.AddActivityListener(listener);

        using var activity = DriveEaseTelemetry.Source.StartActivity("Test.Operation");

        activity.Should().NotBeNull();
        recorded.Should().NotBeNull();
        recorded!.OperationName.Should().Be("Test.Operation");
    }
}

public sealed class TraceContextPropagatorTests
{
    [Fact]
    public void Inject_DoesNothing_WhenNoCurrentActivity()
    {
        var props = new Dictionary<string, object>();

        TraceContextPropagator.Inject(props);

        props.Should().BeEmpty();
    }

    [Fact]
    public void Inject_SetsTraceparent_WhenActivityIsCurrent()
    {
        using var source = new ActivitySource("test-source");
        using var listener = new ActivityListener
        {
            ShouldListenTo = s => s.Name == "test-source",
            Sample = (ref ActivityCreationOptions<ActivityContext> _) =>
                ActivitySamplingResult.AllData
        };
        ActivitySource.AddActivityListener(listener);

        var props = new Dictionary<string, object>();

        using var activity = source.StartActivity("test-op");
        activity.Should().NotBeNull();

        TraceContextPropagator.Inject(props);

        props.Should().ContainKey("traceparent");
        props["traceparent"].Should().Be(activity!.Id);
    }

    [Fact]
    public void Inject_DoesNotSetTracestate_WhenTraceStateIsEmpty()
    {
        using var source = new ActivitySource("test-source-2");
        using var listener = new ActivityListener
        {
            ShouldListenTo = s => s.Name == "test-source-2",
            Sample = (ref ActivityCreationOptions<ActivityContext> _) =>
                ActivitySamplingResult.AllData
        };
        ActivitySource.AddActivityListener(listener);

        var props = new Dictionary<string, object>();

        using var activity = source.StartActivity("test-op");
        // No TraceStateString set — default is empty.

        TraceContextPropagator.Inject(props);

        props.Should().ContainKey("traceparent");
        props.Should().NotContainKey("tracestate");
    }

    [Fact]
    public void Inject_SetsTracestate_WhenTraceStateIsPresent()
    {
        using var source = new ActivitySource("test-source-3");
        using var listener = new ActivityListener
        {
            ShouldListenTo = s => s.Name == "test-source-3",
            Sample = (ref ActivityCreationOptions<ActivityContext> _) =>
                ActivitySamplingResult.AllData
        };
        ActivitySource.AddActivityListener(listener);

        var props = new Dictionary<string, object>();

        using var activity = source.StartActivity("test-op");
        activity!.TraceStateString = "vendor=value";

        TraceContextPropagator.Inject(props);

        props.Should().ContainKey("tracestate");
        props["tracestate"].Should().Be("vendor=value");
    }
}
