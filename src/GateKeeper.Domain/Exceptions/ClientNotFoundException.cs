namespace GateKeeper.Domain.Exceptions;

public class ClientNotFoundException : DomainException
{
    public ClientNotFoundException(string clientId) 
        : base($"Client with ID '{clientId}' was not found.") { }
    
    public ClientNotFoundException(Guid id) 
        : base($"Client with ID '{id}' was not found.") { }
}
