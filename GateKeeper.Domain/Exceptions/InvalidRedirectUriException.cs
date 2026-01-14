namespace GateKeeper.Domain.Exceptions;

public class InvalidRedirectUriException : DomainException
{
    public InvalidRedirectUriException(string uri) 
        : base($"Invalid redirect URI: {uri}") { }
}
