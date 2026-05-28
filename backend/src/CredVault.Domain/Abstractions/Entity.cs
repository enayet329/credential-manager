namespace CredVault.Domain.Abstractions;

/// <summary>
/// Base type for any aggregate root or entity identified by a <see cref="System.Guid"/>.
/// Tracks an in-memory list of <see cref="DomainEvent"/>s raised since the entity was loaded;
/// the unit-of-work flushes and clears these on commit.
/// </summary>
public abstract class Entity
{
    private readonly List<DomainEvent> _domainEvents = [];

    /// <summary>Identity of the entity. Set exactly once by the aggregate's factory.</summary>
    public Guid Id { get; protected init; }

    /// <summary>Domain events raised by this entity that have not yet been dispatched.</summary>
    public IReadOnlyList<DomainEvent> DomainEvents => _domainEvents;

    /// <summary>Appends a domain event to be dispatched after the next successful commit.</summary>
    protected void Raise(DomainEvent @event) => _domainEvents.Add(@event);

    /// <summary>Clears the pending domain-event buffer. Called by the unit-of-work after dispatch.</summary>
    public void ClearDomainEvents() => _domainEvents.Clear();

    /// <inheritdoc/>
    public override bool Equals(object? obj) =>
        obj is Entity other && GetType() == other.GetType() && Id == other.Id;

    /// <inheritdoc/>
    public override int GetHashCode() => HashCode.Combine(GetType(), Id);
}
