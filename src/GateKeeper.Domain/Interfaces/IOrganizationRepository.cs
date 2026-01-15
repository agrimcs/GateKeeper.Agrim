using GateKeeper.Domain.Entities;

namespace GateKeeper.Domain.Interfaces;

public interface IOrganizationRepository
{
    Task<Organization?> GetByIdAsync(Guid id);
    Task<Organization?> GetBySubdomainAsync(string subdomain);
    Task AddAsync(Organization org);
    Task SaveChangesAsync();
    Task<bool> AnyAsync();
}