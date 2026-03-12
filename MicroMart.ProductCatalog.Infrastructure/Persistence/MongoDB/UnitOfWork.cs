using MicroMart.ProductCatalog.Domain.Interfaces;
using MicroMart.ProductCatalog.Infrastructure.Persistence.MongoDB.Documents;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace MicroMart.ProductCatalog.Infrastructure.Persistence.MongoDB;

public sealed class UnitOfWork(MongoDbContext context) : IUnitOfWork
{
    // Pending outbox messages accumulated during the request lifecycle
    private readonly List<OutboxMessage> _pendingOutbox = [];

    // Enqueue an integration event for transactional outbox write
    public void AddOutboxMessage<T>(T integrationEvent) where T : class
    {
        var message = new OutboxMessage
        {
            Type = typeof(T).AssemblyQualifiedName ?? typeof(T).FullName!,
            Payload = JsonSerializer.Serialize(integrationEvent,
                new JsonSerializerOptions { WriteIndented = false })
        };
        _pendingOutbox.Add(message);
    }

    // SaveChangesAsync writes all pending outbox messages atomically.
    // Callers (command handlers) do NOT interact with the session directly.
    public async Task SaveChangesAsync(CancellationToken ct = default)
    {
        if (_pendingOutbox.Count == 0)
        {
            // No outbox messages — no need for a full transaction
            return;
        }

        // Use MongoDB session for atomic outbox write alongside any repo operations
        using var session = await context.StartSessionAsync(ct);
        session.StartTransaction();

        try
        {
            // Insert all pending outbox messages inside the transaction
            await context.OutboxMessages.InsertManyAsync(
                session, _pendingOutbox, cancellationToken: ct);

            await session.CommitTransactionAsync(ct);
            _pendingOutbox.Clear();
        }
        catch
        {
            await session.AbortTransactionAsync(ct);
            throw;
        }
    }


}