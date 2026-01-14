using GateKeeper.Domain.Common;
using GateKeeper.Domain.Events;
using GateKeeper.Domain.Exceptions;
using GateKeeper.Domain.ValueObjects;

namespace GateKeeper.Domain.Entities;

/// <summary>
/// Represents a user account in the GateKeeper OAuth server.
/// This is an aggregate root that encapsulates all user-related business logic.
/// Users can authenticate and authorize OAuth clients to access their resources.
/// </summary>
public class User : AggregateRoot
{
    public Email Email { get; private set; } = null!;
    public string PasswordHash { get; private set; } = string.Empty;
    public string FirstName { get; private set; } = string.Empty;
    public string LastName { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }
    public DateTime? LastLoginAt { get; private set; }
    
    // EF Core constructor
    private User() { }
    
    private User(Guid id, Email email, string passwordHash, string firstName, string lastName)
    {
        Id = id;
        Email = email;
        PasswordHash = passwordHash;
        FirstName = firstName;
        LastName = lastName;
        CreatedAt = DateTime.UtcNow;
    }
    
    /// <summary>
    /// Factory method to register a new user.
    /// Validates business rules and raises UserRegisteredEvent.
    /// </summary>
    public static User Register(
        Email email, 
        string passwordHash, 
        string firstName, 
        string lastName)
    {
        if (string.IsNullOrWhiteSpace(firstName) || firstName.Length > 100)
            throw new DomainException("First name must be between 1 and 100 characters");
            
        if (string.IsNullOrWhiteSpace(lastName) || lastName.Length > 100)
            throw new DomainException("Last name must be between 1 and 100 characters");
        
        var user = new User(Guid.NewGuid(), email, passwordHash, firstName, lastName);
        
        user.AddDomainEvent(new UserRegisteredEvent(user.Id, email.Value));
        
        return user;
    }
    
    public void UpdateProfile(string firstName, string lastName)
    {
        if (string.IsNullOrWhiteSpace(firstName) || firstName.Length > 100)
            throw new DomainException("First name must be between 1 and 100 characters");
            
        if (string.IsNullOrWhiteSpace(lastName) || lastName.Length > 100)
            throw new DomainException("Last name must be between 1 and 100 characters");
        
        FirstName = firstName;
        LastName = lastName;
    }
    
    /// <summary>
    /// Records a successful login and raises UserAuthenticatedEvent.
    /// Updates LastLoginAt timestamp for audit purposes.
    /// </summary>
    public void RecordLogin()
    {
        LastLoginAt = DateTime.UtcNow;
        AddDomainEvent(new UserAuthenticatedEvent(Id, DateTime.UtcNow));
    }
}
