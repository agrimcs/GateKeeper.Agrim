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
        Guid? tenantId = null,
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
        if (tenantId.HasValue)
        {
            // Persist tenant metadata in application properties so we can enforce
            // tenant isolation when resolving clients.
            var json = System.Text.Json.JsonSerializer.SerializeToElement(tenantId.Value.ToString());
            descriptor.Properties["tenant_id"] = json;
        }

        await _applicationManager.CreateAsync(descriptor, cancellationToken);
    }

    /// <inheritdoc />
    public async Task UpdateClientAsync(
        string clientId,
        string displayName,
        IEnumerable<string> redirectUris,
        IEnumerable<string> allowedScopes,
        Guid? tenantId = null,
        CancellationToken cancellationToken = default)
    {
        var application = await _applicationManager.FindByClientIdAsync(clientId, cancellationToken);
        if (application == null)
        {
            throw new InvalidOperationException($"OAuth client with ID '{clientId}' not found.");
        }

        // Populate descriptor from existing application and verify tenant metadata
        var descriptor = new OpenIddictApplicationDescriptor();
        await _applicationManager.PopulateAsync(descriptor, application, cancellationToken);

        // If tenantId provided, verify the application belongs to the tenant
        if (tenantId.HasValue)
        {
            if (!descriptor.Properties.TryGetValue("tenant_id", out var tval))
                throw new InvalidOperationException($"OAuth client '{clientId}' not found for tenant.");

            // Properties store JsonElement values when populated by OpenIddict; handle both string and JsonElement
            if (tval is System.Text.Json.JsonElement je)
            {
                var s = je.ValueKind == System.Text.Json.JsonValueKind.String ? je.GetString() : je.ToString();
                if (s != tenantId.Value.ToString())
                    throw new InvalidOperationException($"OAuth client '{clientId}' not found for tenant.");
            }
            else
            {
                if (tval.ToString() != tenantId.Value.ToString())
                    throw new InvalidOperationException($"OAuth client '{clientId}' not found for tenant.");
            }
        }

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
        if (tenantId.HasValue)
        {
            descriptor.Properties["tenant_id"] = System.Text.Json.JsonSerializer.SerializeToElement(tenantId.Value.ToString());
        }

        await _applicationManager.UpdateAsync(application, descriptor, cancellationToken);
    }

    /// <inheritdoc />
    public async Task DeleteClientAsync(
        string clientId,
        Guid? tenantId = null,
        CancellationToken cancellationToken = default)
    {
        var application = await _applicationManager.FindByClientIdAsync(clientId, cancellationToken);
        if (application == null)
            return;

        if (tenantId.HasValue)
        {
            var descriptor = new OpenIddictApplicationDescriptor();
            await _applicationManager.PopulateAsync(descriptor, application, cancellationToken);
            if (!descriptor.Properties.TryGetValue("tenant_id", out var tval))
                return; // Not the tenant's application, nothing to delete

            if (tval is System.Text.Json.JsonElement je)
            {
                var s = je.ValueKind == System.Text.Json.JsonValueKind.String ? je.GetString() : je.ToString();
                if (s != tenantId.Value.ToString())
                    return; // Not this tenant
            }
            else
            {
                if (tval.ToString() != tenantId.Value.ToString())
                    return; // Not this tenant
            }
        }

        await _applicationManager.DeleteAsync(application, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> ExistsAsync(
        string clientId,
        Guid? tenantId = null,
        CancellationToken cancellationToken = default)
    {
        var application = await _applicationManager.FindByClientIdAsync(clientId, cancellationToken);
        if (application == null)
            return false;

        if (!tenantId.HasValue)
            return true;

        var descriptor = new OpenIddictApplicationDescriptor();
        await _applicationManager.PopulateAsync(descriptor, application, cancellationToken);
        if (!descriptor.Properties.TryGetValue("tenant_id", out var tval))
            return false;

        if (tval is System.Text.Json.JsonElement je)
        {
            var s = je.ValueKind == System.Text.Json.JsonValueKind.String ? je.GetString() : je.ToString();
            return s == tenantId.Value.ToString();
        }

        return tval.ToString() == tenantId.Value.ToString();
    }
}