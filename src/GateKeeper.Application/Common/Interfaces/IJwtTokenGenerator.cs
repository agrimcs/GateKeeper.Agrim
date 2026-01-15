using System.Security.Claims;

namespace GateKeeper.Application.Common.Interfaces;

/// <summary>
/// Interface for generating JWT tokens for authenticated users.
/// </summary>
public interface IJwtTokenGenerator
{
    /// <summary>
    /// Generates a JWT token for the specified user.
    /// </summary>
    /// <param name="userId">The user's unique identifier</param>
    /// <param name="email">The user's email address</param>
    /// <param name="firstName">The user's first name</param>
    /// <param name="lastName">The user's last name</param>
    /// <returns>A JWT token string</returns>
    string GenerateToken(Guid userId, string email, string firstName, string lastName);
    
    /// <summary>
    /// Generates a refresh token for token renewal.
    /// </summary>
    /// <returns>A refresh token string</returns>
    string GenerateRefreshToken();
    
    /// <summary>
    /// Validates a JWT token and returns the claims principal.
    /// </summary>
    /// <param name="token">The JWT token to validate</param>
    /// <returns>ClaimsPrincipal if valid, null otherwise</returns>
    ClaimsPrincipal? ValidateToken(string token);
}
