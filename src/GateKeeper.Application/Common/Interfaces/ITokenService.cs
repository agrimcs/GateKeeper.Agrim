using System.Security.Claims;

namespace GateKeeper.Application.Common.Interfaces;

public interface ITokenService
{
    string GenerateAccessToken(Guid userId, string email, string firstName, string lastName);
    string GenerateRefreshToken();
    ClaimsPrincipal? ValidateToken(string token);
}
