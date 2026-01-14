using GateKeeper.Domain.Entities;

namespace GateKeeper.Domain.Interfaces;

public interface IClientRepository
{
    Task<Client?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Client?> GetByClientIdAsync(string clientId, CancellationToken cancellationToken = default);
    Task<Client?> GetByIdAndOwnerAsync(Guid id, Guid ownerId, CancellationToken cancellationToken = default);
    Task<List<Client>> GetAllByOwnerAsync(Guid ownerId, int skip = 0, int take = 50, CancellationToken cancellationToken = default);
    Task AddAsync(Client client, CancellationToken cancellationToken = default);
    Task UpdateAsync(Client client, CancellationToken cancellationToken = default);
    Task DeleteAsync(Client client, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(string clientId, CancellationToken cancellationToken = default);
}
