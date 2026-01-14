namespace GateKeeper.Domain.Common;

/// <summary>
/// Base record for all value objects in the domain.
/// Value objects are immutable objects identified by their values, not by an ID.
/// Two value objects with the same values are considered equal.
/// Examples: Email, RedirectUri, ClientSecret
/// </summary>
public abstract record ValueObject;
