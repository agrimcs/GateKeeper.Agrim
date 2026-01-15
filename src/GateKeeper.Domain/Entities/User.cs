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
    
    // Multi-tenancy
    public Guid OrganizationId { get; private set; }
    public virtual Organization Organization { get; set; } = null!;
    public bool IsOrganizationAdmin { get; private set; } = false;
    
    // EF Core constructor
    private User() { }
    
    private User(Guid id, Email email, string passwordHash, string firstName, string lastName, Guid organizationId, bool isOrganizationAdmin = false)
    {
        Id = id;
        Email = email;
        PasswordHash = passwordHash;
        FirstName = firstName;
        LastName = lastName;
        CreatedAt = DateTime.UtcNow;
        OrganizationId = organizationId;
        IsOrganizationAdmin = isOrganizationAdmin;
    }
    
    /// <summary>
    /// Factory method to register a new user.
    /// Validates business rules and raises UserRegisteredEvent.
    /// </summary>
    public static User Register(
        Email email, 
        string passwordHash, 
        string firstName, 
        string lastName,
        Guid organizationId,
        bool isOrganizationAdmin = false)
    {
        if (string.IsNullOrWhiteSpace(firstName) || firstName.Length > 100)
            throw new DomainException("First name must be between 1 and 100 characters");
            
        if (string.IsNullOrWhiteSpace(lastName) || lastName.Length > 100)
            throw new DomainException("Last name must be between 1 and 100 characters");
        
        if (organizationId == Guid.Empty)
            throw new DomainException("Organization ID is required");
        
        var user = new User(Guid.NewGuid(), email, passwordHash, firstName, lastName, organizationId, isOrganizationAdmin);
        
        user.AddDomainEvent(new UserRegisteredEvent(user.Id, email.Value));
        
        return user;
    }

    // Compatibility overload: register without organizationId (legacy callers/tests)
    public static User Register(
        Email email,
        string passwordHash,
        string firstName,
        string lastName)
    {
        // Create user with empty Guid; callers should migrate to tenant-aware registration.
        return Register(email, passwordHash, firstName, lastName, Guid.Empty, false);
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
    
    /// <summary>
    /// Promotes user to organization admin role
    /// </summary>
    public void PromoteToOrganizationAdmin()
    {
        IsOrganizationAdmin = true;
    }
    
    /// <summary>
    /// Demotes user from organization admin role
    /// </summary>
    public void DemoteFromOrganizationAdmin()
    {
        IsOrganizationAdmin = false;
    }

    /// <summary>
    /// Sets the OrganizationId for legacy/seed scenarios where a user was created
    /// without an organization. Intended to be called by infrastructure when a
    /// tenant context is available.
    /// </summary>
    public void SetOrganizationId(Guid organizationId)
    {
        if (organizationId == Guid.Empty)
            throw new DomainException("Organization ID cannot be empty");

        OrganizationId = organizationId;
    }
}
