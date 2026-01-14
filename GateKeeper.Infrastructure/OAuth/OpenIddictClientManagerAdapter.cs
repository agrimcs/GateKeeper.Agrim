using GateKeeper.Application.Common.Interfaces;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace GateKeeper.Infrastructure.OAuth;

/// <summary>
/// Adapter that implements IOAuthClientManager using OpenIddict.
/// Translates application-level operations to OpenIddict-specific calls.
/// </summary>
public class OpenIddictClientManagerAdapter : IOAuthClientManager
{
    private readonly IOpenIddictApplicationManager _applicationManager;

    public OpenIddictClientManagerAdapter(IOpenIddictApplicationManager applicationManager)
    {
        _applicationManager = applicationManager;
    }

    /// <inheritdoc />
    public async Task RegisterClientAsync(
        string clientId,
        string displayName,
        string clientType,
        string? clientSecret,
        IEnumerable<string> redirectUris,
        IEnumerable<string> allowedScopes,
        CancellationToken cancellationToken = default)
    {
        var descriptor = new OpenIddictApplicationDescriptor
        {
            ClientId = clientId,
            DisplayName = displayName,
            ClientType = clientType // "public" or "confidential"
        };

        // Set client secret for confidential clients (OpenIddict will hash it)
        if (!string.IsNullOrEmpty(clientSecret))
        {
            descriptor.ClientSecret = clientSecret;
        }

        // Add redirect URIs
        foreach (var uri in redirectUris)
        {
            descriptor.RedirectUris.Add(new Uri(uri));
        }

        // Configure OAuth2/OIDC permissions
        descriptor.Permissions.Add(Permissions.Endpoints.Authorization);
        descriptor.Permissions.Add(Permissions.Endpoints.Token);
        descriptor.Permissions.Add(Permissions.GrantTypes.AuthorizationCode);
        descriptor.Permissions.Add(Permissions.GrantTypes.RefreshToken);
        descriptor.Permissions.Add(Permissions.ResponseTypes.Code);

        // Add scope permissions
        foreach (var scope in allowedScopes)
        {
            descriptor.Permissions.Add($"{Permissions.Prefixes.Scope}{scope}");
        }

        // Require PKCE for public clients
        if (clientType == ClientTypes.Public)
        {
            descriptor.Requirements.Add(Requirements.Features.ProofKeyForCodeExchange);
        }

        // Create application in OpenIddict
        await _applicationManager.CreateAsync(descriptor, cancellationToken);
    }

    /// <inheritdoc />
    public async Task UpdateClientAsync(
        string clientId,
        string displayName,
        IEnumerable<string> redirectUris,
        IEnumerable<string> allowedScopes,
        CancellationToken cancellationToken = default)
    {
        var application = await _applicationManager.FindByClientIdAsync(clientId, cancellationToken);
        if (application == null)
        {
            throw new InvalidOperationException($"OAuth client with ID '{clientId}' not found.");
        }

        // Populate descriptor from existing application
        var descriptor = new OpenIddictApplicationDescriptor();
        await _applicationManager.PopulateAsync(descriptor, application, cancellationToken);

        // Update properties
        descriptor.DisplayName = displayName;

        // Update redirect URIs
        descriptor.RedirectUris.Clear();
        foreach (var uri in redirectUris)
        {
            descriptor.RedirectUris.Add(new Uri(uri));
        }

        // Update permissions
        descriptor.Permissions.Clear();
        descriptor.Permissions.Add(Permissions.Endpoints.Authorization);
        descriptor.Permissions.Add(Permissions.Endpoints.Token);
        descriptor.Permissions.Add(Permissions.GrantTypes.AuthorizationCode);
        descriptor.Permissions.Add(Permissions.GrantTypes.RefreshToken);
        descriptor.Permissions.Add(Permissions.ResponseTypes.Code);

        foreach (var scope in allowedScopes)
        {
            descriptor.Permissions.Add($"{Permissions.Prefixes.Scope}{scope}");
        }

        // Apply updates
        await _applicationManager.UpdateAsync(application, descriptor, cancellationToken);
    }

    /// <inheritdoc />
    public async Task DeleteClientAsync(
        string clientId,
        CancellationToken cancellationToken = default)
    {
        var application = await _applicationManager.FindByClientIdAsync(clientId, cancellationToken);
        if (application != null)
        {
            await _applicationManager.DeleteAsync(application, cancellationToken);
        }
    }

    /// <inheritdoc />
    public async Task<bool> ExistsAsync(
        string clientId,
        CancellationToken cancellationToken = default)
    {
        var application = await _applicationManager.FindByClientIdAsync(clientId, cancellationToken);
        return application != null;
    }
}