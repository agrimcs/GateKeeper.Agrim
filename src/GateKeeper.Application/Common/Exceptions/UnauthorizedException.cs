namespace GateKeeper.Application.Common.Exceptions;

/// <summary>
/// Exception thrown when authentication fails.
/// </summary>
public class UnauthorizedException : ApplicationException
{
    public UnauthorizedException(string message) : base(message) { }
}
