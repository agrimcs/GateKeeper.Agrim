namespace GateKeeper.Application.Users.DTOs;

/// <summary>
/// Response DTO for user profile information.
/// </summary>
public record UserProfileDto
{
    public Guid Id { get; init; }
    public string Email { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public DateTime? LastLoginAt { get; init; }
}
