namespace GateKeeper.Domain.Common;

/// <summary>
/// Base class for all entities in the domain.
/// Entities are objects that have a unique identity (ID) that persists over time.
/// Two entities are equal if they have the same ID, regardless of their property values.
/// </summary>
public abstract class Entity
{
    public Guid Id { get; protected set; }
    
    protected Entity() { }
    
    protected Entity(Guid id)
    {
        Id = id;
    }
    
    public override bool Equals(object? obj)
    {
        if (obj is not Entity entity)
            return false;
            
        return Id == entity.Id;
    }
    
    public override int GetHashCode() => Id.GetHashCode();
}
