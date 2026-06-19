using DriveEase.Shared.Domain;
using System.Text.Json;

namespace DriveEase.Shared.Outbox;

public sealed class OutboxMessage
{
    public Guid Id { get; init; }
    public string EventType { get; init; } = string.Empty;
    public string Payload { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public DateTime? ProcessedAt { get; set; }
    public string? Error { get; set; }

    public static OutboxMessage From(IIntegrationEvent integrationEvent) => new()
    {
        Id = Guid.NewGuid(),
        EventType = integrationEvent.GetType().AssemblyQualifiedName!,
        Payload = JsonSerializer.Serialize(integrationEvent, integrationEvent.GetType()),
        CreatedAt = DateTime.UtcNow
    };
}
