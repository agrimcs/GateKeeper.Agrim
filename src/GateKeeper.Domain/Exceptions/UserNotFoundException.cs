namespace GateKeeper.Domain.Exceptions;

/// <summary>
/// Exception thrown when a user cannot be found by ID or email.
/// </summary>
public class UserNotFoundException : DomainException
{
    public UserNotFoundException(Guid userId) 
        : base($"User with ID '{userId}' was not found") { }
    
    public UserNotFoundException(string email) 
        : base($"User with email '{email}' was not found") { }
}
