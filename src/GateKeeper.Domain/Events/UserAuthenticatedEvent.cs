using GateKeeper.Domain.Common;

namespace GateKeeper.Domain.Events;

public record UserAuthenticatedEvent(Guid UserId, DateTime AuthenticatedAt) : IDomainEvent;
