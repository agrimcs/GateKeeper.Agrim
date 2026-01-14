namespace GateKeeper.Application.Users.DTOs;

/// <summary>
/// Request DTO for updating user profile.
/// </summary>
public record UpdateUserProfileDto
{
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
}
