using GateKeeper.Application.Common;
using GateKeeper.Domain.Entities;
using System.Threading.Tasks;

namespace GateKeeper.Application.Common
{
    // Minimal tenant service used for compatibility when DI isn't available (tests, legacy code)
    public class NullTenantService : ITenantService
    {
        public Guid? GetCurrentTenantId() => null;

        public Task<Organization?> GetCurrentTenantAsync() => Task.FromResult<Organization?>(null);

        public bool HasTenantContext() => false;
    }
}
