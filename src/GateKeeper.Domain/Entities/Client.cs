using GateKeeper.Domain.Common;
using GateKeeper.Domain.Enums;
using GateKeeper.Domain.Events;
using GateKeeper.Domain.Exceptions;
using GateKeeper.Domain.ValueObjects;

namespace GateKeeper.Domain.Entities;

/// <summary>
/// Represents an OAuth 2.0 client application that can request access tokens.
/// This is an aggregate root that manages client configuration, secrets, and redirect URIs.
/// Clients can be Public (mobile/JavaScript apps) or Confidential (server-side apps with secrets).
/// </summary>
public class Client : AggregateRoot
{
    public string ClientId { get; private set; } = string.Empty;
    public ClientSecret? Secret { get; private set; }
    public string DisplayName { get; private set; } = string.Empty;
    public ClientType Type { get; private set; }
    public Guid OwnerId { get; private set; }
    public Guid OrganizationId { get; private set; }
    public DateTime CreatedAt { get; private set; }
    
    private readonly List<RedirectUri> _redirectUris = new();
    public IReadOnlyCollection<RedirectUri> RedirectUris => _redirectUris.AsReadOnly();
    
    private readonly List<string> _allowedScopes = new();
    public IReadOnlyCollection<string> AllowedScopes => _allowedScopes.AsReadOnly();
    
    // EF Core constructor
    private Client() { }
    
    private Client(
        Guid id,
        string clientId,
        string displayName,
        ClientType type,
        Guid ownerId,
        Guid organizationId,
        ClientSecret? secret = null)
    {
        Id = id;
        ClientId = clientId;
        DisplayName = displayName;
        Type = type;
        OwnerId = ownerId;
        OrganizationId = organizationId;
        Secret = secret;
        CreatedAt = DateTime.UtcNow;
    }
    
    /// <summary>
    /// Factory method to create a confidential OAuth client (server-side app).
    /// Confidential clients have a secret and can authenticate themselves.
    /// Examples: backend web applications, server-to-server integrations.
    /// </summary>
    public static Client CreateConfidential(
        string displayName,
        string clientId,
        ClientSecret secret,
        Guid ownerId,
        Guid organizationId,
        IEnumerable<RedirectUri> redirectUris,
        IEnumerable<string> scopes)
    {
        if (string.IsNullOrWhiteSpace(displayName))
            throw new DomainException("Client display name is required");
            
        if (string.IsNullOrWhiteSpace(clientId))
            throw new DomainException("Client ID is required");
        
        var client = new Client(Guid.NewGuid(), clientId, displayName, ClientType.Confidential, ownerId, organizationId, secret);
        
        foreach (var uri in redirectUris)
            client._redirectUris.Add(uri);
            
        foreach (var scope in scopes)
            client._allowedScopes.Add(scope);
        
        client.AddDomainEvent(new ClientRegisteredEvent(client.Id, client.ClientId));
        
        return client;
    }

    // Compatibility overload: older signature without organizationId (6 args)
    public static Client CreateConfidential(
        string displayName,
        string clientId,
        ClientSecret secret,
        Guid ownerId,
        IEnumerable<RedirectUri> redirectUris,
        IEnumerable<string> scopes)
    {
        return CreateConfidential(displayName, clientId, secret, ownerId, Guid.Empty, redirectUris, scopes);
    }

    // Compatibility overload for older signature where organizationId was last
    public static Client CreateConfidential(
        string displayName,
        string clientId,
        ClientSecret secret,
        Guid ownerId,
        IEnumerable<RedirectUri> redirectUris,
        IEnumerable<string> scopes,
        Guid organizationId)
    {
        return CreateConfidential(displayName, clientId, secret, ownerId, organizationId, redirectUris, scopes);
    }
    
    /// <summary>
    /// Factory method to create a public OAuth client (browser/mobile app).
    /// Public clients cannot securely store secrets and rely on PKCE for security.
    /// Examples: single-page apps (React/Angular), mobile apps, desktop apps.
    /// </summary>
    public static Client CreatePublic(
        string displayName,
        string clientId,
        Guid ownerId,
        Guid organizationId,
        IEnumerable<RedirectUri> redirectUris,
        IEnumerable<string> scopes)
    {
        if (string.IsNullOrWhiteSpace(displayName))
            throw new DomainException("Client display name is required");
            
        if (string.IsNullOrWhiteSpace(clientId))
            throw new DomainException("Client ID is required");
        
        var client = new Client(Guid.NewGuid(), clientId, displayName, ClientType.Public, ownerId, organizationId);
        
        foreach (var uri in redirectUris)
            client._redirectUris.Add(uri);
            
        foreach (var scope in scopes)
            client._allowedScopes.Add(scope);
        
        client.AddDomainEvent(new ClientRegisteredEvent(client.Id, client.ClientId));
        
        return client;
    }

    // Compatibility overload: older signature without organizationId (5 args)
    public static Client CreatePublic(
        string displayName,
        string clientId,
        Guid ownerId,
        IEnumerable<RedirectUri> redirectUris,
        IEnumerable<string> scopes)
    {
        return CreatePublic(displayName, clientId, ownerId, Guid.Empty, redirectUris, scopes);
    }

    // Compatibility overload for older signature where organizationId was last
    public static Client CreatePublic(
        string displayName,
        string clientId,
        Guid ownerId,
        IEnumerable<RedirectUri> redirectUris,
        IEnumerable<string> scopes,
        Guid organizationId)
    {
        return CreatePublic(displayName, clientId, ownerId, organizationId, redirectUris, scopes);
    }
    
    public void AddRedirectUri(RedirectUri uri)
    {
        if (_redirectUris.Any(u => u.Value.Equals(uri.Value, StringComparison.Ordinal)))
            throw new DomainException($"Redirect URI {uri.Value} already exists for this client");
        
        _redirectUris.Add(uri);
    }
    
    public void RemoveRedirectUri(RedirectUri uri)
    {
        var existing = _redirectUris.FirstOrDefault(u => u.Value == uri.Value);
        if (existing == null)
            throw new DomainException($"Redirect URI {uri.Value} not found");
        
        _redirectUris.Remove(existing);
    }
    
    /// <summary>
    /// Validates if a redirect URI is registered for this client.
    /// Critical for OAuth security to prevent authorization code interception.
    /// </summary>
    public bool ValidateRedirectUri(string uri)
    {
        return _redirectUris.Any(u => u.Value.Equals(uri, StringComparison.Ordinal));
    }
    
    public void UpdateDisplayName(string displayName)
    {
        if (string.IsNullOrWhiteSpace(displayName))
            throw new DomainException("Display name cannot be empty");
        
        DisplayName = displayName;
    }
    
    /// <summary>
    /// Validates if the client is owned by the specified user.
    /// Used for authorization checks before operations.
    /// </summary>
    public bool IsOwnedBy(Guid userId)
    {
        return OwnerId == userId;
    }

    /// <summary>
    /// Sets the OrganizationId for legacy/seed scenarios where a client was
    /// created without an organization. Intended for infrastructure use when
    /// a tenant context is available.
    /// </summary>
    public void SetOrganizationId(Guid organizationId)
    {
        if (organizationId == Guid.Empty)
            throw new DomainException("Organization ID cannot be empty");

        OrganizationId = organizationId;
    }
}
