using DriveEase.Shared.Domain;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace DriveEase.Shared.Outbox;

// Runs inside every SaveChangesAsync call.
// Collects domain events from tracked aggregates, serialises them as OutboxMessage rows,
// and adds them to the same DbContext so they are saved in the same transaction.
public sealed class OutboxInterceptor : SaveChangesInterceptor
{
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        var context = eventData.Context!;

        var aggregates = context.ChangeTracker
            .Entries<IHasDomainEvents>()
            .Where(e => e.Entity.DomainEvents.Count > 0)
            .ToList();

        var messages = aggregates
            .SelectMany(e => e.Entity.DomainEvents)
            .OfType<IIntegrationEvent>()
            .Select(OutboxMessage.From)
            .ToList();

        foreach (var msg in messages)
            context.Add(msg);

        foreach (var entry in aggregates)
            entry.Entity.ClearDomainEvents();

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }
}
