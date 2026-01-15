using BCrypt.Net;
using GateKeeper.Domain.Interfaces;

namespace GateKeeper.Infrastructure.Security;

/// <summary>
/// BCrypt implementation of password hashing.
/// Uses work factor of 12 (OWASP recommended).
/// </summary>
public class BcryptPasswordHasher : IPasswordHasher
{
    private const int WorkFactor = 12;

    /// <summary>
    /// Hashes a plain text password using BCrypt with automatic salt generation.
    /// </summary>
    /// <param name="password">Plain text password to hash</param>
    /// <returns>BCrypt hashed password</returns>
    public string HashPassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            throw new ArgumentException("Password cannot be null or empty", nameof(password));
        }

        return BCrypt.Net.BCrypt.HashPassword(password, WorkFactor);
    }

    /// <summary>
    /// Verifies a plain text password against a BCrypt hash.
    /// </summary>
    /// <param name="hashedPassword">BCrypt hashed password from database</param>
    /// <param name="providedPassword">Plain text password to verify</param>
    /// <returns>True if password matches, false otherwise</returns>
    public bool VerifyPassword(string hashedPassword, string providedPassword)
    {
        if (string.IsNullOrWhiteSpace(hashedPassword))
        {
            throw new ArgumentException("Hashed password cannot be null or empty", nameof(hashedPassword));
        }

        if (string.IsNullOrWhiteSpace(providedPassword))
        {
            return false;
        }

        try
        {
            return BCrypt.Net.BCrypt.Verify(providedPassword, hashedPassword);
        }
        catch (SaltParseException)
        {
            // Invalid hash format
            return false;
        }
    }
}
