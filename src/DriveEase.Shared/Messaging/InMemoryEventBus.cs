using DriveEase.Shared.Domain;
using Microsoft.Extensions.DependencyInjection;

namespace DriveEase.Shared.Messaging;

// Registered as singleton, so IServiceProvider would be the root provider.
// Using IServiceScopeFactory ensures scoped handlers (e.g. notification handlers)
// are resolved from a child scope rather than the root, avoiding the
// "cannot resolve scoped service from root provider" error.
public sealed class InMemoryEventBus(IServiceScopeFactory scopeFactory) : IEventBus
{
    public async Task PublishAsync<T>(T integrationEvent, CancellationToken cancellationToken = default)
        where T : IIntegrationEvent
    {
        using var scope = scopeFactory.CreateScope();
        var handlers = scope.ServiceProvider.GetServices<IIntegrationEventHandler<T>>();
        foreach (var handler in handlers)
            await handler.HandleAsync(integrationEvent, cancellationToken);
    }
}

public interface IIntegrationEventHandler<in T> where T : IIntegrationEvent
{
    Task HandleAsync(T integrationEvent, CancellationToken cancellationToken = default);
}
