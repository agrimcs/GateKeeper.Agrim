using GateKeeper.Domain.Entities;

namespace GateKeeper.Domain.Interfaces;

public interface IClientRepository
{
    Task<Client?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Client?> GetByClientIdAsync(string clientId, CancellationToken cancellationToken = default);
    Task<List<Client>> GetAllAsync(int skip = 0, int take = 50, CancellationToken cancellationToken = default);
    Task AddAsync(Client client, CancellationToken cancellationToken = default);
    Task UpdateAsync(Client client, CancellationToken cancellationToken = default);
    Task DeleteAsync(Client client, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(string clientId, CancellationToken cancellationToken = default);
}
