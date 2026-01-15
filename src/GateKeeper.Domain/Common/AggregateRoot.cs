namespace GateKeeper.Domain.Common;

/// <summary>
/// Base class for aggregate roots in the domain.
/// An aggregate root is the main entity that controls a consistency boundary.
/// All changes to objects within the aggregate must go through the root.
/// Aggregate roots can raise domain events to communicate with other aggregates.
/// Examples: User, Client
/// </summary>
public abstract class AggregateRoot : Entity
{
    private readonly List<IDomainEvent> _domainEvents = new();
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();
    
    protected void AddDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }
    
    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
}
