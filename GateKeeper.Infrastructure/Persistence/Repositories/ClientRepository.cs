using GateKeeper.Domain.Entities;
using GateKeeper.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace GateKeeper.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository implementation for Client aggregate.
/// Provides data access operations for OAuth clients.
/// </summary>
public class ClientRepository : IClientRepository
{
    private readonly ApplicationDbContext _context;

    public ClientRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Client?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Clients
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task<Client?> GetByClientIdAsync(
        string clientId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Clients
            .FirstOrDefaultAsync(c => c.ClientId == clientId, cancellationToken);
    }

    public async Task<bool> ExistsAsync(string clientId, CancellationToken cancellationToken = default)
    {
        return await _context.Clients
            .AnyAsync(c => c.ClientId == clientId, cancellationToken);
    }

    public async Task<List<Client>> GetAllAsync(
        int skip = 0,
        int take = 50,
        CancellationToken cancellationToken = default)
    {
        return await _context.Clients
            .OrderByDescending(c => c.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Client client, CancellationToken cancellationToken = default)
    {
        await _context.Clients.AddAsync(client, cancellationToken);
    }

    public Task UpdateAsync(Client client, CancellationToken cancellationToken = default)
    {
        _context.Clients.Update(client);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Client client, CancellationToken cancellationToken = default)
    {
        _context.Clients.Remove(client);
        return Task.CompletedTask;
    }
}
