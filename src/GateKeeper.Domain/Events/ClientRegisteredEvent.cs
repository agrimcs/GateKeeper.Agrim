using GateKeeper.Domain.Common;

namespace GateKeeper.Domain.Events;

public record ClientRegisteredEvent(Guid Id, string ClientId) : IDomainEvent;
