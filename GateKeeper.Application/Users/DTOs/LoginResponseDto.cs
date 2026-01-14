namespace GateKeeper.Application.Users.DTOs;

/// <summary>
/// Response DTO returned after successful login.
/// Contains the JWT token and user profile information.
/// </summary>
public class LoginResponseDto
{
    /// <summary>
    /// JWT token for authenticating subsequent requests
    /// </summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// User profile information
    /// </summary>
    public UserProfileDto User { get; set; } = null!;
    
    /// <summary>
    /// Token type (always "Bearer" for JWT)
    /// </summary>
    public string TokenType { get; set; } = "Bearer";
    
    /// <summary>
    /// Token expiration time in seconds
    /// </summary>
    public int ExpiresIn { get; set; } = 3600;
}
