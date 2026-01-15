using GateKeeper.Domain.Entities;
using GateKeeper.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace GateKeeper.Infrastructure.Persistence.Repositories;

public class OrganizationRepository : IOrganizationRepository
{
    private readonly ApplicationDbContext _db;

    public OrganizationRepository(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task AddAsync(Organization org)
    {
        await _db.Set<Organization>().AddAsync(org);
    }

    public async Task<Organization?> GetByIdAsync(Guid id)
    {
        return await _db.Set<Organization>().FindAsync(id);
    }

    public async Task<Organization?> GetBySubdomainAsync(string subdomain)
    {
        return await _db.Set<Organization>().FirstOrDefaultAsync(o => o.Subdomain == subdomain);
    }

    public async Task<bool> AnyAsync()
    {
        return await _db.Set<Organization>().AnyAsync();
    }

    public async Task SaveChangesAsync()
    {
        await _db.SaveChangesAsync();
    }
}
