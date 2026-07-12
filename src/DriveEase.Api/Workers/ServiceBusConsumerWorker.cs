using Azure.Identity;
using Azure.Messaging.ServiceBus;
using DriveEase.Enrollments.Domain.Events;
using DriveEase.Lessons.Domain.Events;
using DriveEase.Shared.Domain;
using DriveEase.Shared.Messaging;
using System.Text.Json;

namespace DriveEase.Api.Workers;

// AzureServiceBusEventBus only publishes integration events to Service Bus topics —
// nothing previously read them back off the wire, so every IIntegrationEventHandler
// (instructor/student notifications) silently never fired once Service Bus replaced
// InMemoryEventBus. This worker is the missing consumer half: one processor per
// subscription, dispatching each message to the matching IIntegrationEventHandler<T>
// by its Subject (set to typeof(T).Name in AzureServiceBusEventBus.PublishAsync).
//
// Subscriptions have no SQL filters, so a single subscription per topic receives
// every event type published to that topic — one processor per topic is sufficient.
public sealed class ServiceBusConsumerWorker(
    string fullyQualifiedNamespace,
    IServiceScopeFactory scopeFactory,
    ILogger<ServiceBusConsumerWorker> logger) : BackgroundService
{
    private static readonly (string Topic, string Subscription)[] Subscriptions =
    [
        ("enrollment-events", "enrollment-confirmed"),
        ("lesson-events",     "lesson-completed"),
    ];

    private static readonly Dictionary<string, Type> EventTypes = new()
    {
        [nameof(EnrollmentConfirmedEvent)] = typeof(EnrollmentConfirmedEvent),
        [nameof(PaymentFailedEvent)]       = typeof(PaymentFailedEvent),
        [nameof(EnrollmentCancelledEvent)] = typeof(EnrollmentCancelledEvent),
        [nameof(InstructorAssignedEvent)]  = typeof(InstructorAssignedEvent),
        [nameof(LessonBookedEvent)]        = typeof(LessonBookedEvent),
        [nameof(LessonCompletedEvent)]     = typeof(LessonCompletedEvent),
        [nameof(LessonCancelledEvent)]     = typeof(LessonCancelledEvent),
    };

    private ServiceBusClient? _client;
    private readonly List<ServiceBusProcessor> _processors = [];

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        _client = new ServiceBusClient(fullyQualifiedNamespace, new DefaultAzureCredential());

        foreach (var (topic, subscription) in Subscriptions)
        {
            var processor = _client.CreateProcessor(topic, subscription, new ServiceBusProcessorOptions
            {
                AutoCompleteMessages = false,
                MaxConcurrentCalls   = 5
            });

            processor.ProcessMessageAsync += OnMessageAsync;
            processor.ProcessErrorAsync   += OnErrorAsync;
            _processors.Add(processor);

            await processor.StartProcessingAsync(cancellationToken);
            logger.LogInformation("ServiceBusConsumerWorker listening on {Topic}/{Subscription}", topic, subscription);
        }

        await base.StartAsync(cancellationToken);
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken) => Task.CompletedTask;

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        foreach (var processor in _processors)
            await processor.StopProcessingAsync(cancellationToken);
        await base.StopAsync(cancellationToken);
    }

    public override async void Dispose()
    {
        foreach (var processor in _processors)
            await processor.DisposeAsync();
        if (_client is not null)
            await _client.DisposeAsync();
    }

    private async Task OnMessageAsync(ProcessMessageEventArgs args)
    {
        var subject = args.Message.Subject;

        if (subject is null || !EventTypes.TryGetValue(subject, out var eventType))
        {
            logger.LogWarning("Unknown message subject {Subject} — completing without processing", subject);
            await args.CompleteMessageAsync(args.Message, args.CancellationToken);
            return;
        }

        var integrationEvent = JsonSerializer.Deserialize(args.Message.Body.ToArray(), eventType)
            ?? throw new InvalidOperationException($"Failed to deserialize message body as {eventType.Name}.");

        using var scope = scopeFactory.CreateScope();
        var handlerType = typeof(IIntegrationEventHandler<>).MakeGenericType(eventType);
        var handleMethod = handlerType.GetMethod(nameof(IIntegrationEventHandler<IIntegrationEvent>.HandleAsync))!;

        foreach (var handler in scope.ServiceProvider.GetServices(handlerType))
        {
            await (Task)handleMethod.Invoke(handler, [integrationEvent, args.CancellationToken])!;
        }

        await args.CompleteMessageAsync(args.Message, args.CancellationToken);
        logger.LogInformation("Consumed {Subject} from {Subscription}", subject, args.Identifier);
    }

    private Task OnErrorAsync(ProcessErrorEventArgs args)
    {
        logger.LogError(args.Exception, "ServiceBusConsumerWorker error on {Subscription}", args.EntityPath);
        return Task.CompletedTask;
    }
}
