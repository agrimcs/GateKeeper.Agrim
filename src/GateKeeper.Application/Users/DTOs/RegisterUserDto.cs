namespace GateKeeper.Application.Users.DTOs;

/// <summary>
/// Request DTO for user registration.
/// </summary>
public record RegisterUserDto
{
    public string Email { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public string ConfirmPassword { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    
    // Organization details for new organization creation
    public string? OrganizationName { get; init; }
    public string? OrganizationSubdomain { get; init; }
}
