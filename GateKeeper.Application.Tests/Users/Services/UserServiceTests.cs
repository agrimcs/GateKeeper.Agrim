using FluentAssertions;
using GateKeeper.Application.Common.Exceptions;
using GateKeeper.Application.Users.DTOs;
using GateKeeper.Application.Users.Services;
using GateKeeper.Domain.Entities;
using GateKeeper.Domain.Exceptions;
using GateKeeper.Domain.Interfaces;
using GateKeeper.Domain.ValueObjects;
using Moq;

namespace GateKeeper.Application.Tests.Users.Services;

/// <summary>
/// Tests for UserService application logic.
/// Uses Moq for mocking dependencies (repositories, infrastructure services).
/// </summary>
public class UserServiceTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IPasswordHasher> _passwordHasherMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly UserService _userService;

    public UserServiceTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _passwordHasherMock = new Mock<IPasswordHasher>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();

        _userService = new UserService(
            _userRepositoryMock.Object,
            _passwordHasherMock.Object,
            _unitOfWorkMock.Object);
    }

    #region RegisterAsync Tests

    [Fact]
    public async Task RegisterAsync_WithValidData_ShouldCreateUser()
    {
        // Arrange
        var dto = new RegisterUserDto
        {
            Email = "newuser@example.com",
            Password = "SecurePass123!",
            ConfirmPassword = "SecurePass123!",
            FirstName = "John",
            LastName = "Doe"
        };

        _userRepositoryMock
            .Setup(x => x.ExistsAsync(It.IsAny<Email>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _passwordHasherMock
            .Setup(x => x.HashPassword(dto.Password))
            .Returns("hashed-password");

        _userRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _userService.RegisterAsync(dto);

        // Assert
        result.Should().NotBeNull();
        result.Email.Should().Be(dto.Email.ToLowerInvariant());
        result.FirstName.Should().Be(dto.FirstName);
        result.LastName.Should().Be(dto.LastName);
        result.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

        _userRepositoryMock.Verify(x => x.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RegisterAsync_WithExistingEmail_ShouldThrowDuplicateEmailException()
    {
        // Arrange
        var dto = new RegisterUserDto
        {
            Email = "existing@example.com",
            Password = "SecurePass123!",
            ConfirmPassword = "SecurePass123!",
            FirstName = "John",
            LastName = "Doe"
        };

        _userRepositoryMock
            .Setup(x => x.ExistsAsync(It.IsAny<Email>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        Func<Task> act = async () => await _userService.RegisterAsync(dto);

        // Assert
        await act.Should().ThrowAsync<DuplicateEmailException>()
            .WithMessage("*existing@example.com*");

        _userRepositoryMock.Verify(x => x.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RegisterAsync_WithInvalidEmail_ShouldThrowDomainException()
    {
        // Arrange
        var dto = new RegisterUserDto
        {
            Email = "invalid-email",
            Password = "SecurePass123!",
            ConfirmPassword = "SecurePass123!",
            FirstName = "John",
            LastName = "Doe"
        };

        // Act
        Func<Task> act = async () => await _userService.RegisterAsync(dto);

        // Assert
        await act.Should().ThrowAsync<DomainException>();
    }

    #endregion

    #region LoginAsync Tests

    [Fact]
    public async Task LoginAsync_WithValidCredentials_ShouldReturnUserProfile()
    {
        // Arrange
        var dto = new LoginUserDto
        {
            Email = "user@example.com",
            Password = "CorrectPassword123!"
        };

        var email = Email.Create(dto.Email);
        var user = User.Register(email, "hashed-password", "John", "Doe");

        _userRepositoryMock
            .Setup(x => x.GetByEmailAsync(It.IsAny<Email>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _passwordHasherMock
            .Setup(x => x.VerifyPassword(user.PasswordHash, dto.Password))
            .Returns(true);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _userService.LoginAsync(dto);

        // Assert
        result.Should().NotBeNull();
        result.Email.Should().Be(dto.Email.ToLowerInvariant());
        result.Id.Should().Be(user.Id);
        result.LastLoginAt.Should().NotBeNull();

        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task LoginAsync_WithInvalidEmail_ShouldThrowUnauthorizedException()
    {
        // Arrange
        var dto = new LoginUserDto
        {
            Email = "nonexistent@example.com",
            Password = "Password123!"
        };

        _userRepositoryMock
            .Setup(x => x.GetByEmailAsync(It.IsAny<Email>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act
        Func<Task> act = async () => await _userService.LoginAsync(dto);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedException>()
            .WithMessage("*Invalid email or password*");
    }

    [Fact]
    public async Task LoginAsync_WithInvalidPassword_ShouldThrowUnauthorizedException()
    {
        // Arrange
        var dto = new LoginUserDto
        {
            Email = "user@example.com",
            Password = "WrongPassword123!"
        };

        var email = Email.Create(dto.Email);
        var user = User.Register(email, "hashed-password", "John", "Doe");

        _userRepositoryMock
            .Setup(x => x.GetByEmailAsync(It.IsAny<Email>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _passwordHasherMock
            .Setup(x => x.VerifyPassword(user.PasswordHash, dto.Password))
            .Returns(false);

        // Act
        Func<Task> act = async () => await _userService.LoginAsync(dto);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedException>()
            .WithMessage("*Invalid email or password*");

        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region GetProfileAsync Tests

    [Fact]
    public async Task GetProfileAsync_WithExistingUser_ShouldReturnProfile()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var email = Email.Create("user@example.com");
        var user = User.Register(email, "hashed-password", "John", "Doe");

        _userRepositoryMock
            .Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await _userService.GetProfileAsync(userId);

        // Assert
        result.Should().NotBeNull();
        result.Email.Should().Be("user@example.com");
        result.FirstName.Should().Be("John");
        result.LastName.Should().Be("Doe");
    }

    [Fact]
    public async Task GetProfileAsync_WithNonExistentUser_ShouldThrowUserNotFoundException()
    {
        // Arrange
        var userId = Guid.NewGuid();

        _userRepositoryMock
            .Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act
        Func<Task> act = async () => await _userService.GetProfileAsync(userId);

        // Assert
        await act.Should().ThrowAsync<UserNotFoundException>();
    }

    #endregion

    #region UpdateProfileAsync Tests

    [Fact]
    public async Task UpdateProfileAsync_WithValidData_ShouldUpdateProfile()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var email = Email.Create("user@example.com");
        var user = User.Register(email, "hashed-password", "John", "Doe");

        var updateDto = new UpdateUserProfileDto
        {
            FirstName = "Jane",
            LastName = "Smith"
        };

        _userRepositoryMock
            .Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _userService.UpdateProfileAsync(userId, updateDto);

        // Assert
        result.Should().NotBeNull();
        result.FirstName.Should().Be("Jane");
        result.LastName.Should().Be("Smith");

        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateProfileAsync_WithNonExistentUser_ShouldThrowUserNotFoundException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var updateDto = new UpdateUserProfileDto
        {
            FirstName = "Jane",
            LastName = "Smith"
        };

        _userRepositoryMock
            .Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act
        Func<Task> act = async () => await _userService.UpdateProfileAsync(userId, updateDto);

        // Assert
        await act.Should().ThrowAsync<UserNotFoundException>();
    }

    #endregion

    #region GetAllUsersAsync Tests

    [Fact]
    public async Task GetAllUsersAsync_ShouldReturnUserList()
    {
        // Arrange
        var users = new List<User>
        {
            User.Register(Email.Create("user1@example.com"), "hash1", "John", "Doe"),
            User.Register(Email.Create("user2@example.com"), "hash2", "Jane", "Smith")
        };

        _userRepositoryMock
            .Setup(x => x.GetAllAsync(0, 50, It.IsAny<CancellationToken>()))
            .ReturnsAsync(users);

        // Act
        var result = await _userService.GetAllUsersAsync();

        // Assert
        result.Should().HaveCount(2);
        result[0].Email.Should().Be("user1@example.com");
        result[1].Email.Should().Be("user2@example.com");
    }

    [Fact]
    public async Task GetAllUsersAsync_WithCustomPagination_ShouldUseProvidedParameters()
    {
        // Arrange
        var users = new List<User>
        {
            User.Register(Email.Create("user3@example.com"), "hash3", "Bob", "Wilson")
        };

        _userRepositoryMock
            .Setup(x => x.GetAllAsync(10, 5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(users);

        // Act
        var result = await _userService.GetAllUsersAsync(skip: 10, take: 5);

        // Assert
        result.Should().HaveCount(1);
        _userRepositoryMock.Verify(x => x.GetAllAsync(10, 5, It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion
}
