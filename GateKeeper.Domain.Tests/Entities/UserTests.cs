using FluentAssertions;
using GateKeeper.Domain.Entities;
using GateKeeper.Domain.Events;
using GateKeeper.Domain.Exceptions;
using GateKeeper.Domain.ValueObjects;

namespace GateKeeper.Domain.Tests.Entities;

/// <summary>
/// Tests for User aggregate root entity.
/// Validates business rules, domain events, and user lifecycle.
/// </summary>
public class UserTests
{
    [Fact]
    public void Register_WithValidData_ShouldCreateUser()
    {
        // Arrange
        var email = Email.Create("user@example.com");
        var passwordHash = "hashed-password";
        var firstName = "John";
        var lastName = "Doe";

        // Act
        var user = User.Register(email, passwordHash, firstName, lastName);

        // Assert
        user.Should().NotBeNull();
        user.Id.Should().NotBeEmpty();
        user.Email.Should().Be(email);
        user.PasswordHash.Should().Be(passwordHash);
        user.FirstName.Should().Be(firstName);
        user.LastName.Should().Be(lastName);
        user.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        user.LastLoginAt.Should().BeNull();
    }

    [Fact]
    public void Register_ShouldRaiseUserRegisteredEvent()
    {
        // Arrange
        var email = Email.Create("user@example.com");

        // Act
        var user = User.Register(email, "hashed-password", "John", "Doe");

        // Assert
        user.DomainEvents.Should().ContainSingle();
        var domainEvent = user.DomainEvents.First();
        domainEvent.Should().BeOfType<UserRegisteredEvent>();
        
        var userRegisteredEvent = (UserRegisteredEvent)domainEvent;
        userRegisteredEvent.UserId.Should().Be(user.Id);
        userRegisteredEvent.Email.Should().Be(email.Value);
    }

    [Theory]
    [InlineData(null, "Doe")]
    [InlineData("", "Doe")]
    [InlineData("   ", "Doe")]
    public void Register_WithInvalidFirstName_ShouldThrowDomainException(string invalidFirstName, string lastName)
    {
        // Arrange
        var email = Email.Create("user@example.com");

        // Act
        Action act = () => User.Register(email, "hashed-password", invalidFirstName, lastName);

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("First name must be between 1 and 100 characters");
    }

    [Theory]
    [InlineData("John", null)]
    [InlineData("John", "")]
    [InlineData("John", "   ")]
    public void Register_WithInvalidLastName_ShouldThrowDomainException(string firstName, string invalidLastName)
    {
        // Arrange
        var email = Email.Create("user@example.com");

        // Act
        Action act = () => User.Register(email, "hashed-password", firstName, invalidLastName);

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("Last name must be between 1 and 100 characters");
    }

    [Fact]
    public void UpdateProfile_WithValidData_ShouldUpdateNames()
    {
        // Arrange
        var email = Email.Create("user@example.com");
        var user = User.Register(email, "hashed-password", "John", "Doe");

        // Act
        user.UpdateProfile("Jane", "Smith");

        // Assert
        user.FirstName.Should().Be("Jane");
        user.LastName.Should().Be("Smith");
    }

    [Theory]
    [InlineData(null, "Smith")]
    [InlineData("", "Smith")]
    [InlineData("   ", "Smith")]
    public void UpdateProfile_WithInvalidFirstName_ShouldThrowDomainException(string invalidFirstName, string lastName)
    {
        // Arrange
        var email = Email.Create("user@example.com");
        var user = User.Register(email, "hashed-password", "John", "Doe");

        // Act
        Action act = () => user.UpdateProfile(invalidFirstName, lastName);

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("First name must be between 1 and 100 characters");
    }

    [Theory]
    [InlineData("Jane", null)]
    [InlineData("Jane", "")]
    [InlineData("Jane", "   ")]
    public void UpdateProfile_WithInvalidLastName_ShouldThrowDomainException(string firstName, string invalidLastName)
    {
        // Arrange
        var email = Email.Create("user@example.com");
        var user = User.Register(email, "hashed-password", "John", "Doe");

        // Act
        Action act = () => user.UpdateProfile(firstName, invalidLastName);

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("Last name must be between 1 and 100 characters");
    }

    [Fact]
    public void RecordLogin_ShouldUpdateLastLoginAt()
    {
        // Arrange
        var email = Email.Create("user@example.com");
        var user = User.Register(email, "hashed-password", "John", "Doe");
        user.ClearDomainEvents(); // Clear registration event

        // Act
        user.RecordLogin();

        // Assert
        user.LastLoginAt.Should().NotBeNull();
        user.LastLoginAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void RecordLogin_ShouldRaiseUserAuthenticatedEvent()
    {
        // Arrange
        var email = Email.Create("user@example.com");
        var user = User.Register(email, "hashed-password", "John", "Doe");
        user.ClearDomainEvents(); // Clear registration event

        // Act
        user.RecordLogin();

        // Assert
        user.DomainEvents.Should().ContainSingle();
        var domainEvent = user.DomainEvents.First();
        domainEvent.Should().BeOfType<UserAuthenticatedEvent>();
        
        var userAuthenticatedEvent = (UserAuthenticatedEvent)domainEvent;
        userAuthenticatedEvent.UserId.Should().Be(user.Id);
        userAuthenticatedEvent.AuthenticatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void ClearDomainEvents_ShouldRemoveAllEvents()
    {
        // Arrange
        var email = Email.Create("user@example.com");
        var user = User.Register(email, "hashed-password", "John", "Doe");
        user.DomainEvents.Should().NotBeEmpty();

        // Act
        user.ClearDomainEvents();

        // Assert
        user.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void User_WithSameId_ShouldBeEqual()
    {
        // Arrange
        var email = Email.Create("user@example.com");
        var user1 = User.Register(email, "hash", "John", "Doe");
        
        // Create another reference to same user (simulate loading from DB)
        var user2 = User.Register(email, "hash", "Jane", "Smith");
        // Manually set same ID for testing equality
        typeof(User).GetProperty("Id")!.SetValue(user2, user1.Id);

        // Assert
        user1.Should().Be(user2);
    }
}
