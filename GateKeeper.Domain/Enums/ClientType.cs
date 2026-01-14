namespace GateKeeper.Domain.Enums;

public enum ClientType
{
    Public = 0,      // JavaScript/Mobile apps (no secret)
    Confidential = 1  // Server-side apps (has secret)
}
