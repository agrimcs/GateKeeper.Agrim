using GateKeeper.Domain.Common;
using GateKeeper.Domain.Exceptions;
using System.Net.Mail;
using System.Text.RegularExpressions;

namespace GateKeeper.Domain.ValueObjects;

/// <summary>
/// Value object representing a validated email address.
/// Ensures email is in valid format and normalized (lowercase, trimmed).
/// Uses OWASP-compliant validation for production identity provider security.
/// Immutable - once created, the email cannot be changed.
/// </summary>
public sealed record Email : ValueObject
{
    // OWASP-recommended email regex pattern
    // Requires: local@domain.tld format with proper TLD
    private static readonly Regex EmailRegex = new(
        @"^[a-z0-9.!#$%&'*+/=?^_`{|}~-]+@[a-z0-9](?:[a-z0-9-]{0,61}[a-z0-9])?(?:\.[a-z0-9](?:[a-z0-9-]{0,61}[a-z0-9])?)*\.[a-z]{2,}$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase,
        TimeSpan.FromMilliseconds(100));
    
    public string Value { get; init; }
    
    private Email(string value)
    {
        Value = value;
    }
    
    public static Email Create(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new DomainException("Email cannot be empty");
            
        email = email.Trim().ToLowerInvariant();
        
        // Length validation per OWASP recommendations
        if (email.Length > 254)
            throw new DomainException("Email address is too long (max 254 characters)");
        
        // Step 1: Use MailAddress for basic RFC compliance and injection protection
        if (!MailAddress.TryCreate(email, out _))
            throw new DomainException("Invalid email format");
        
        // Step 2: Apply stricter OWASP validation requiring proper TLD
        if (!EmailRegex.IsMatch(email))
            throw new DomainException("Invalid email format - must be a valid internet email address");
            
        return new Email(email);
    }
}
