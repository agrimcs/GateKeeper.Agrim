using GateKeeper.Domain.Enums;

namespace GateKeeper.Application.Clients.DTOs;

/// <summary>
/// Request DTO for registering a new OAuth client.
/// </summary>
public record RegisterClientDto
{
    public string DisplayName { get; init; } = string.Empty;
    public ClientType Type { get; init; }
    public List<string> RedirectUris { get; init; } = new();
    public List<string> AllowedScopes { get; init; } = new();
}
