namespace GateKeeper.Application.Users.DTOs;

/// <summary>
/// Request DTO for user login.
/// </summary>
public record LoginUserDto
{
    public string Email { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
}
