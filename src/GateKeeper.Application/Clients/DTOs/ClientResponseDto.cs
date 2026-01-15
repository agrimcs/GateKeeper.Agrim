using GateKeeper.Domain.Enums;

namespace GateKeeper.Application.Clients.DTOs;

/// <summary>
/// Response DTO for OAuth client information.
/// Includes secret only on creation for confidential clients.
/// </summary>
public record ClientResponseDto
{
    public Guid Id { get; init; }
    public string ClientId { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public ClientType Type { get; init; }
    public List<string> RedirectUris { get; init; } = new();
    public List<string> AllowedScopes { get; init; } = new();
    public DateTime CreatedAt { get; init; }
    
    /// <summary>
    /// Plain-text client secret. Only populated on creation for confidential clients.
    /// WARNING: This should be displayed to user only once and never stored in plain text.
    /// </summary>
    public string? PlainTextSecret { get; init; }
}
