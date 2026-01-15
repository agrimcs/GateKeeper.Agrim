using GateKeeper.Domain.Entities;
using GateKeeper.Domain.Interfaces;
using GateKeeper.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

using GateKeeper.Application.Common;

namespace GateKeeper.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository implementation for User aggregate.
/// Provides data access operations for users.
/// </summary>
public class UserRepository : IUserRepository
{
    private readonly ApplicationDbContext _context;
    private readonly ITenantService _tenantService;

    public UserRepository(ApplicationDbContext context, ITenantService tenantService)
    {
        _context = context;
        _tenantService = tenantService;
    }

    public async Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
    }

    public async Task<User?> GetByEmailAsync(Email email, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .FirstOrDefaultAsync(u => u.Email.Value == email.Value, cancellationToken);
    }

    public async Task<bool> ExistsAsync(Email email, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .AnyAsync(u => u.Email.Value == email.Value, cancellationToken);
    }

    public async Task<List<User>> GetAllAsync(
        int skip = 0,
        int take = 50,
        CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .OrderByDescending(u => u.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(User user, CancellationToken cancellationToken = default)
    {
        // If the user was created without an OrganizationId (legacy/seed),
        // set it from the current tenant context when available.
        if (user.OrganizationId == Guid.Empty)
        {
            var tenantId = _tenantService.GetCurrentTenantId();
            if (tenantId.HasValue)
            {
                user.SetOrganizationId(tenantId.Value);
            }
        }

        await _context.Users.AddAsync(user, cancellationToken);
    }

    public Task UpdateAsync(User user, CancellationToken cancellationToken = default)
    {
        _context.Users.Update(user);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(User user, CancellationToken cancellationToken = default)
    {
        _context.Users.Remove(user);
        return Task.CompletedTask;
    }
}
