using GateKeeper.Domain.Entities;

namespace GateKeeper.Application.Common;

public interface ITenantService
{
    Guid? GetCurrentTenantId();
    Task<Organization?> GetCurrentTenantAsync();
    bool HasTenantContext();
}