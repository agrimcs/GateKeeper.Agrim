namespace GateKeeper.Application.Clients.DTOs;

/// <summary>
/// Request DTO for updating an OAuth client.
/// </summary>
public record UpdateClientDto
{
    public string DisplayName { get; init; } = string.Empty;
    public List<string> RedirectUris { get; init; } = new();
}
