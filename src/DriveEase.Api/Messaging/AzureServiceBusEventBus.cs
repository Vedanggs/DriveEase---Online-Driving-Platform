using Azure.Identity;
using Azure.Messaging.ServiceBus;
using DriveEase.Shared.Domain;
using DriveEase.Shared.Messaging;
using System.Text.Json;

namespace DriveEase.Api.Messaging;

// Publishes integration events to Azure Service Bus topics using the App Service
// system-assigned Managed Identity — no SAS key or connection string required.
public sealed class AzureServiceBusEventBus : IEventBus, IAsyncDisposable
{
    private readonly ServiceBusClient _client;
    private readonly Dictionary<string, ServiceBusSender> _senders = new();

    public AzureServiceBusEventBus(string fullyQualifiedNamespace)
    {
        // DefaultAzureCredential picks up the system-assigned MI automatically
        // when running on Azure App Service.
        _client = new ServiceBusClient(fullyQualifiedNamespace, new DefaultAzureCredential());
    }

    public async Task PublishAsync<T>(T integrationEvent, CancellationToken cancellationToken = default)
        where T : IIntegrationEvent
    {
        var topic = TopicFor(typeof(T));

        if (!_senders.TryGetValue(topic, out var sender))
        {
            sender = _client.CreateSender(topic);
            _senders[topic] = sender;
        }

        var message = new ServiceBusMessage(JsonSerializer.SerializeToUtf8Bytes(integrationEvent))
        {
            Subject     = typeof(T).Name,
            ContentType = "application/json"
        };

        await sender.SendMessageAsync(message, cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var sender in _senders.Values)
            await sender.DisposeAsync();
        await _client.DisposeAsync();
    }

    // Map event type namespace to Service Bus topic name.
    private static string TopicFor(Type t) => t.Namespace switch
    {
        string ns when ns.Contains("Enrollment", StringComparison.Ordinal) => "enrollment-events",
        string ns when ns.Contains("Lesson",     StringComparison.Ordinal) => "lesson-events",
        _ => "enrollment-events"
    };
}
