using GateKeeper.Domain.Entities;
using GateKeeper.Domain.ValueObjects;

namespace GateKeeper.Domain.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<User?> GetByEmailAsync(Email email, CancellationToken cancellationToken = default);
    Task<List<User>> GetAllAsync(int skip = 0, int take = 50, CancellationToken cancellationToken = default);
    Task AddAsync(User user, CancellationToken cancellationToken = default);
    Task UpdateAsync(User user, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Email email, CancellationToken cancellationToken = default);
}
