using GateKeeper.Domain.Common;
using GateKeeper.Domain.Exceptions;

namespace GateKeeper.Domain.ValueObjects;

/// <summary>
/// Value object representing a validated OAuth redirect URI.
/// Enforces absolute URLs and HTTPS requirement (except localhost for development).
/// Critical for OAuth security - prevents authorization code interception attacks.
/// </summary>
public sealed record RedirectUri : ValueObject
{
    public string Value { get; init; }
    
    private RedirectUri(string value)
    {
        Value = value;
    }
    
    public static RedirectUri Create(string uri)
    {
        if (string.IsNullOrWhiteSpace(uri))
            throw new DomainException("Redirect URI cannot be empty");
            
        if (!Uri.TryCreate(uri, UriKind.Absolute, out var parsedUri))
            throw new InvalidRedirectUriException(uri);
            
        // For OAuth security, we typically require HTTPS in production
        // For development, we can allow http://localhost
        if (parsedUri.Scheme != "https" && 
            !parsedUri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidRedirectUriException($"{uri} - HTTPS required for non-localhost URIs");
        }
            
        return new RedirectUri(uri);
    }
}
