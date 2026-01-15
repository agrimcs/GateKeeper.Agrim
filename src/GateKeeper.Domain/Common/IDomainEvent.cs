namespace GateKeeper.Domain.Common;

/// <summary>
/// Marker interface for domain events.
/// Domain events represent something significant that happened in the domain.
/// They are used for loose coupling between aggregates and for audit trails.
/// Examples: UserRegisteredEvent, ClientRegisteredEvent
/// </summary>
public interface IDomainEvent
{
    DateTime OccurredOn => DateTime.UtcNow;
}
