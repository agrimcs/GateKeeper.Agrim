using GateKeeper.Domain.Common;
using System.Security.Cryptography;

namespace GateKeeper.Domain.ValueObjects;

/// <summary>
/// Value object representing an OAuth client secret.
/// Generates cryptographically secure random secrets for confidential clients.
/// In production, secrets should be hashed (BCrypt) before storage.
/// </summary>
public sealed record ClientSecret : ValueObject
{
    public string HashedValue { get; init; }
    
    private ClientSecret(string hashedValue)
    {
        HashedValue = hashedValue;
    }
    
    /// <summary>
    /// Generates a new cryptographically secure client secret.
    /// NOTE: This returns a plain-text secret. The Application/Infrastructure layer
    /// is responsible for hashing this secret using IPasswordHasher before persistence.
    /// This maintains zero dependencies in the Domain layer.
    /// </summary>
    public static ClientSecret Generate()
    {
        // Generate a cryptographically secure random secret
        var randomBytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        var plainSecret = Convert.ToBase64String(randomBytes);
        
        // Return plain secret - Application layer will hash it before storage
        return new ClientSecret(plainSecret);
    }
    
    public static ClientSecret FromHashed(string hashedValue)
    {
        return new ClientSecret(hashedValue);
    }
}
