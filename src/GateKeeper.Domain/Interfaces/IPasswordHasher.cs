namespace GateKeeper.Domain.Interfaces;

/// <summary>
/// Interface for password hashing operations.
/// Implementation will use BCrypt.Net-Next in the Infrastructure layer.
/// </summary>
public interface IPasswordHasher
{
    /// <summary>
    /// Hashes a plain-text password using the bcrypt algorithm.
    /// </summary>
    /// <param name="password">Plain-text password to hash</param>
    /// <returns>Bcrypt hashed password with embedded salt</returns>
    string HashPassword(string password);
    
    /// <summary>
    /// Verifies a plain-text password against a bcrypt hash.
    /// </summary>
    /// <param name="hashedPassword">The bcrypt hash to verify against</param>
    /// <param name="providedPassword">The plain-text password to verify</param>
    /// <returns>True if password matches, false otherwise</returns>
    bool VerifyPassword(string hashedPassword, string providedPassword);
}
