using FluentAssertions;
using GateKeeper.Domain.Entities;
using GateKeeper.Domain.ValueObjects;
using GateKeeper.Infrastructure.Persistence;
using GateKeeper.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;

namespace GateKeeper.Infrastructure.Tests.Repositories;

public class UserRepositoryTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly UserRepository _repository;

    public UserRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _repository = new UserRepository(_context, new GateKeeper.Application.Common.NullTenantService());
    }

    [Fact]
    public async Task AddAsync_ShouldAddUserToDatabase()
    {
        // Arrange
        var email = Email.Create("test@example.com");
        var user = User.Register(email, "hashedPassword", "John", "Doe");

        // Act
        await _repository.AddAsync(user);
        await _context.SaveChangesAsync();

        // Assert
        var savedUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == user.Id);
        savedUser.Should().NotBeNull();
        savedUser!.Email.Value.Should().Be("test@example.com");
        savedUser.FirstName.Should().Be("John");
    }

    [Fact]
    public async Task GetByEmailAsync_ShouldReturnUser_WhenUserExists()
    {
        // Arrange
        var email = Email.Create("test@example.com");
        var user = User.Register(email, "hashedPassword", "Jane", "Smith");
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByEmailAsync(email);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(user.Id);
        result.FirstName.Should().Be("Jane");
    }

    [Fact]
    public async Task GetByEmailAsync_ShouldReturnNull_WhenUserDoesNotExist()
    {
        // Arrange
        var email = Email.Create("nonexistent@example.com");

        // Act
        var result = await _repository.GetByEmailAsync(email);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task ExistsAsync_ShouldReturnTrue_WhenUserExists()
    {
        // Arrange
        var email = Email.Create("existing@example.com");
        var user = User.Register(email, "hashedPassword", "Bob", "Johnson");
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        // Act
        var exists = await _repository.ExistsAsync(email);

        // Assert
        exists.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsAsync_ShouldReturnFalse_WhenUserDoesNotExist()
    {
        // Arrange
        var email = Email.Create("nonexistent@example.com");

        // Act
        var exists = await _repository.ExistsAsync(email);

        // Assert
        exists.Should().BeFalse();
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnUsersOrderedByCreatedAt()
    {
        // Arrange
        var user1 = User.Register(Email.Create("user1@test.com"), "hash", "User", "One");
        var user2 = User.Register(Email.Create("user2@test.com"), "hash", "User", "Two");
        await _context.Users.AddRangeAsync(user1, user2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetAllAsync();

        // Assert
        result.Should().HaveCount(2);
        result.Should().BeInDescendingOrder(u => u.CreatedAt);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
