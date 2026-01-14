namespace GateKeeper.Application.Common.Interfaces;

/// <summary>
/// Service to access current authenticated user information.
/// Will be implemented in Presentation layer (Server).
/// </summary>
public interface ICurrentUserService
{
    Guid? UserId { get; }
    string? Email { get; }
    bool IsAuthenticated { get; }
}
