using GateKeeper.Domain.Common;

namespace GateKeeper.Domain.Events;

public record UserRegisteredEvent(Guid UserId, string Email) : IDomainEvent;
