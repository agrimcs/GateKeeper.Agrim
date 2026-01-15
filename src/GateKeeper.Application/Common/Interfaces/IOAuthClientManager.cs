namespace GateKeeper.Application.Common.Interfaces;

/// <summary>
/// Application-level abstraction for managing OAuth client applications.
/// Decouples the application layer from specific OAuth implementation (OpenIddict).
/// </summary>
public interface IOAuthClientManager
{
    /// <summary>
    /// Registers a new OAuth client application with the OAuth server.
    /// </summary>
    /// <param name="clientId">Unique client identifier</param>
    /// <param name="displayName">Human-readable display name</param>
    /// <param name="clientType">Type of client (Public or Confidential)</param>
    /// <param name="clientSecret">Client secret for confidential clients (plain text, will be hashed)</param>
    /// <param name="redirectUris">List of allowed redirect URIs</param>
    /// <param name="allowedScopes">List of allowed OAuth scopes</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task RegisterClientAsync(
        string clientId,
        string displayName,
        string clientType,
        string? clientSecret,
        IEnumerable<string> redirectUris,
        IEnumerable<string> allowedScopes,
        Guid? tenantId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing OAuth client application.
    /// </summary>
    /// <param name="clientId">Unique client identifier</param>
    /// <param name="displayName">Updated display name</param>
    /// <param name="redirectUris">Updated list of redirect URIs</param>
    /// <param name="allowedScopes">Updated list of allowed scopes</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task UpdateClientAsync(
        string clientId,
        string displayName,
        IEnumerable<string> redirectUris,
        IEnumerable<string> allowedScopes,
        Guid? tenantId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes an OAuth client application from the OAuth server.
    /// </summary>
    /// <param name="clientId">Unique client identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task DeleteClientAsync(
        string clientId,
        Guid? tenantId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a client with the specified client ID exists.
    /// </summary>
    /// <param name="clientId">Client identifier to check</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if client exists, false otherwise</returns>
    Task<bool> ExistsAsync(
        string clientId,
        Guid? tenantId = null,
        CancellationToken cancellationToken = default);
}