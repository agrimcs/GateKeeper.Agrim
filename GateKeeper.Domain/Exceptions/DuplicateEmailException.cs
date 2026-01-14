namespace GateKeeper.Domain.Exceptions;

/// <summary>
/// Exception thrown when attempting to register a user with an email that already exists.
/// </summary>
public class DuplicateEmailException : DomainException
{
    public DuplicateEmailException(string email) 
        : base($"A user with email '{email}' already exists") { }
}
