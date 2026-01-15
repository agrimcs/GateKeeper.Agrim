using FluentAssertions;
using GateKeeper.Application.Clients.DTOs;
using GateKeeper.Application.Clients.Services;
using GateKeeper.Application.Common.Interfaces;
using GateKeeper.Domain.Entities;
using GateKeeper.Domain.Enums;
using GateKeeper.Domain.Exceptions;
using GateKeeper.Domain.Interfaces;
using GateKeeper.Domain.ValueObjects;
using Moq;

namespace GateKeeper.Application.Tests.Clients.Services;

/// <summary>
/// Tests for ClientService application logic.
/// </summary>
public class ClientServiceTests
{
    private readonly Mock<IClientRepository> _clientRepositoryMock;
    private readonly Mock<IPasswordHasher> _passwordHasherMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IOAuthClientManager> _oauthClientManagerMock;
    private readonly ClientService _clientService;
    private readonly Guid _testOwnerId = Guid.NewGuid();

    public ClientServiceTests()
    {
        _clientRepositoryMock = new Mock<IClientRepository>();
        _passwordHasherMock = new Mock<IPasswordHasher>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _oauthClientManagerMock = new Mock<IOAuthClientManager>();

        _clientService = new ClientService(
            _clientRepositoryMock.Object,
            _passwordHasherMock.Object,
            _unitOfWorkMock.Object,
            _oauthClientManagerMock.Object);
    }

    #region RegisterClientAsync Tests

    [Fact]
    public async Task RegisterClientAsync_WithPublicClient_ShouldCreateClientWithoutSecret()
    {
        // Arrange
        var dto = new RegisterClientDto
        {
            DisplayName = "My Public App",
            Type = ClientType.Public,
            RedirectUris = new List<string> { "https://example.com/callback" },
            AllowedScopes = new List<string> { "openid", "profile" }
        };

        _clientRepositoryMock
            .Setup(x => x.ExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _oauthClientManagerMock
            .Setup(x => x.ExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _clientRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Client>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _oauthClientManagerMock
            .Setup(x => x.RegisterClientAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _clientService.RegisterClientAsync(dto, _testOwnerId);

        // Assert
        result.Should().NotBeNull();
        result.DisplayName.Should().Be("My Public App");
        result.Type.Should().Be(ClientType.Public);
        result.ClientId.Should().Be("my-public-app");
        result.PlainTextSecret.Should().BeNull();
        result.RedirectUris.Should().Contain("https://example.com/callback");
        result.AllowedScopes.Should().BeEquivalentTo("openid", "profile");

        _clientRepositoryMock.Verify(x => x.AddAsync(It.IsAny<Client>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

        // Verify OAuth registration was called
        _oauthClientManagerMock.Verify(x => x.RegisterClientAsync(
            "my-public-app",
            "My Public App",
            "public",
            null,
            It.Is<IEnumerable<string>>(uris => uris.Contains("https://example.com/callback")),
            It.Is<IEnumerable<string>>(scopes => scopes.Contains("openid") && scopes.Contains("profile")),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RegisterClientAsync_WithConfidentialClient_ShouldCreateClientWithSecret()
    {
        // Arrange
        var dto = new RegisterClientDto
        {
            DisplayName = "My Confidential App",
            Type = ClientType.Confidential,
            RedirectUris = new List<string> { "https://example.com/callback" },
            AllowedScopes = new List<string> { "openid", "profile", "email" }
        };

        _clientRepositoryMock
            .Setup(x => x.ExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _oauthClientManagerMock
            .Setup(x => x.ExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _passwordHasherMock
            .Setup(x => x.HashPassword(It.IsAny<string>()))
            .Returns("hashed-secret");

        _clientRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Client>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _oauthClientManagerMock
            .Setup(x => x.RegisterClientAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _clientService.RegisterClientAsync(dto, _testOwnerId);

        // Assert
        result.Should().NotBeNull();
        result.DisplayName.Should().Be("My Confidential App");
        result.Type.Should().Be(ClientType.Confidential);
        result.ClientId.Should().Be("my-confidential-app");
        result.PlainTextSecret.Should().NotBeNullOrEmpty();
        result.RedirectUris.Should().Contain("https://example.com/callback");
        result.AllowedScopes.Should().BeEquivalentTo("openid", "profile", "email");

        _passwordHasherMock.Verify(x => x.HashPassword(It.IsAny<string>()), Times.Once);

        // Verify OAuth registration was called with secret
        _oauthClientManagerMock.Verify(x => x.RegisterClientAsync(
            "my-confidential-app",
            "My Confidential App",
            "confidential",
            It.Is<string>(s => !string.IsNullOrEmpty(s)), // Secret should be provided
            It.Is<IEnumerable<string>>(uris => uris.Contains("https://example.com/callback")),
            It.Is<IEnumerable<string>>(scopes => scopes.Contains("openid") && scopes.Contains("profile") && scopes.Contains("email")),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RegisterClientAsync_WithDuplicateClientId_ShouldGenerateUniqueId()
    {
        // Arrange
        var dto = new RegisterClientDto
        {
            DisplayName = "My App",
            Type = ClientType.Public,
            RedirectUris = new List<string> { "https://example.com/callback" },
            AllowedScopes = new List<string> { "openid" }
        };

        _clientRepositoryMock
            .SetupSequence(x => x.ExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true)  // First call: client ID exists
            .ReturnsAsync(false); // Second call: unique client ID

        _oauthClientManagerMock
            .Setup(x => x.ExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _clientRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Client>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _oauthClientManagerMock
            .Setup(x => x.RegisterClientAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _clientService.RegisterClientAsync(dto, _testOwnerId);

        // Assert
        result.Should().NotBeNull();
        result.ClientId.Should().StartWith("my-app-");
        result.ClientId.Length.Should().BeGreaterThan("my-app".Length);

        // Verify checked both domain and OAuth existence
        _clientRepositoryMock.Verify(x => x.ExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task RegisterClientAsync_WhenOAuthClientExists_ShouldGenerateUniqueId()
    {
        // Arrange
        var dto = new RegisterClientDto
        {
            DisplayName = "My App",
            Type = ClientType.Public,
            RedirectUris = new List<string> { "https://example.com/callback" },
            AllowedScopes = new List<string> { "openid" }
        };

        _clientRepositoryMock
            .Setup(x => x.ExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _oauthClientManagerMock
            .SetupSequence(x => x.ExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true)  // First call: OAuth client exists
            .ReturnsAsync(false); // Second call: unique client ID

        _clientRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Client>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _oauthClientManagerMock
            .Setup(x => x.RegisterClientAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _clientService.RegisterClientAsync(dto, _testOwnerId);

        // Assert
        result.Should().NotBeNull();
        result.ClientId.Should().StartWith("my-app-");

        // Verify OAuth existence was checked
        _oauthClientManagerMock.Verify(x => x.ExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task RegisterClientAsync_WhenOAuthRegistrationFails_ShouldPropagateException()
    {
        // Arrange
        var dto = new RegisterClientDto
        {
            DisplayName = "My App",
            Type = ClientType.Public,
            RedirectUris = new List<string> { "https://example.com/callback" },
            AllowedScopes = new List<string> { "openid" }
        };

        _clientRepositoryMock
            .Setup(x => x.ExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _oauthClientManagerMock
            .Setup(x => x.ExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _clientRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Client>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _oauthClientManagerMock
            .Setup(x => x.RegisterClientAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("OAuth registration failed"));

        // Act
        Func<Task> act = async () => await _clientService.RegisterClientAsync(dto, _testOwnerId);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("OAuth registration failed");
    }

    [Fact]
    public async Task RegisterClientAsync_WithSpecialCharactersInName_ShouldSanitizeClientId()
    {
        // Arrange
        var dto = new RegisterClientDto
        {
            DisplayName = "My @PP! #123",
            Type = ClientType.Public,
            RedirectUris = new List<string> { "https://example.com/callback" },
            AllowedScopes = new List<string> { "openid" }
        };

        _clientRepositoryMock
            .Setup(x => x.ExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _oauthClientManagerMock
            .Setup(x => x.ExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _clientRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Client>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _oauthClientManagerMock
            .Setup(x => x.RegisterClientAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _clientService.RegisterClientAsync(dto, _testOwnerId);

        // Assert
        result.ClientId.Should().Be("my-pp-123");
    }

    #endregion

    #region GetClientByIdAsync Tests

    [Fact]
    public async Task GetClientByIdAsync_WithExistingClient_ShouldReturnClient()
    {
        // Arrange
        var clientId = Guid.NewGuid();
        var redirectUri = RedirectUri.Create("https://example.com/callback");
        var client = Client.CreatePublic(
            "Test App",
            "test-app",
            _testOwnerId,
            new List<RedirectUri> { redirectUri },
            new List<string> { "openid" });

        _clientRepositoryMock
            .Setup(x => x.GetByIdAndOwnerAsync(clientId, _testOwnerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(client);

        // Act
        var result = await _clientService.GetClientByIdAsync(clientId, _testOwnerId);

        // Assert
        result.Should().NotBeNull();
        result.DisplayName.Should().Be("Test App");
        result.ClientId.Should().Be("test-app");
        result.PlainTextSecret.Should().BeNull(); // Secret only returned on creation
    }

    [Fact]
    public async Task GetClientByIdAsync_WithNonExistentClient_ShouldThrowClientNotFoundException()
    {
        // Arrange
        var clientId = Guid.NewGuid();

        _clientRepositoryMock
            .Setup(x => x.GetByIdAndOwnerAsync(clientId, _testOwnerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Client?)null);

        // Act
        Func<Task> act = async () => await _clientService.GetClientByIdAsync(clientId, _testOwnerId);

        // Assert
        await act.Should().ThrowAsync<ClientNotFoundException>();
    }

    [Fact]
    public async Task GetClientByIdAsync_WithWrongOwner_ShouldThrowClientNotFoundException()
    {
        // Arrange
        var clientId = Guid.NewGuid();
        var wrongOwnerId = Guid.NewGuid();

        _clientRepositoryMock
            .Setup(x => x.GetByIdAndOwnerAsync(clientId, wrongOwnerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Client?)null);

        // Act
        Func<Task> act = async () => await _clientService.GetClientByIdAsync(clientId, wrongOwnerId);

        // Assert
        await act.Should().ThrowAsync<ClientNotFoundException>();
    }

    #endregion

    #region GetClientByClientIdAsync Tests

    [Fact]
    public async Task GetClientByClientIdAsync_WithExistingClient_ShouldReturnClient()
    {
        // Arrange
        var clientIdString = "test-app";
        var redirectUri = RedirectUri.Create("https://example.com/callback");
        var client = Client.CreatePublic(
            "Test App",
            clientIdString,
            _testOwnerId,
            new List<RedirectUri> { redirectUri },
            new List<string> { "openid" });

        _clientRepositoryMock
            .Setup(x => x.GetByClientIdAsync(clientIdString, It.IsAny<CancellationToken>()))
            .ReturnsAsync(client);

        // Act
        var result = await _clientService.GetClientByClientIdAsync(clientIdString);

        // Assert
        result.Should().NotBeNull();
        result.ClientId.Should().Be(clientIdString);
    }

    [Fact]
    public async Task GetClientByClientIdAsync_WithNonExistentClient_ShouldThrowClientNotFoundException()
    {
        // Arrange
        var clientIdString = "nonexistent-app";

        _clientRepositoryMock
            .Setup(x => x.GetByClientIdAsync(clientIdString, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Client?)null);

        // Act
        Func<Task> act = async () => await _clientService.GetClientByClientIdAsync(clientIdString);

        // Assert
        await act.Should().ThrowAsync<ClientNotFoundException>();
    }

    #endregion

    #region GetAllClientsAsync Tests

    [Fact]
    public async Task GetAllClientsAsync_ShouldReturnClientList()
    {
        // Arrange
        var redirectUri = RedirectUri.Create("https://example.com/callback");
        var clients = new List<Client>
        {
            Client.CreatePublic("App 1", "app-1", _testOwnerId, new List<RedirectUri> { redirectUri }, new List<string> { "openid" }),
            Client.CreatePublic("App 2", "app-2", _testOwnerId, new List<RedirectUri> { redirectUri }, new List<string> { "openid" })
        };

        _clientRepositoryMock
            .Setup(x => x.GetAllByOwnerAsync(_testOwnerId, 0, 50, It.IsAny<CancellationToken>()))
            .ReturnsAsync(clients);

        // Act
        var result = await _clientService.GetAllClientsAsync(_testOwnerId);

        // Assert
        result.Should().HaveCount(2);
        result[0].DisplayName.Should().Be("App 1");
        result[1].DisplayName.Should().Be("App 2");
    }

    [Fact]
    public async Task GetAllClientsAsync_WithCustomPagination_ShouldUseProvidedParameters()
    {
        // Arrange
        var redirectUri = RedirectUri.Create("https://example.com/callback");
        var clients = new List<Client>
        {
            Client.CreatePublic("App 3", "app-3", _testOwnerId, new List<RedirectUri> { redirectUri }, new List<string> { "openid" })
        };

        _clientRepositoryMock
            .Setup(x => x.GetAllByOwnerAsync(_testOwnerId, 10, 5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(clients);

        // Act
        var result = await _clientService.GetAllClientsAsync(_testOwnerId, skip: 10, take: 5);

        // Assert
        result.Should().HaveCount(1);
        _clientRepositoryMock.Verify(x => x.GetAllByOwnerAsync(_testOwnerId, 10, 5, It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region UpdateClientAsync Tests

    [Fact]
    public async Task UpdateClientAsync_WithValidData_ShouldUpdateClientAndOAuthServer()
    {
        // Arrange
        var clientId = Guid.NewGuid();
        var redirectUri = RedirectUri.Create("https://example.com/callback");
        var client = Client.CreatePublic(
            "Old Name",
            "test-app",
            _testOwnerId,
            new List<RedirectUri> { redirectUri },
            new List<string> { "openid" });

        var updateDto = new UpdateClientDto
        {
            DisplayName = "New Name",
            RedirectUris = new List<string> { "https://example.com/new-callback" }
        };

        _clientRepositoryMock
            .Setup(x => x.GetByIdAndOwnerAsync(clientId, _testOwnerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(client);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _oauthClientManagerMock
            .Setup(x => x.UpdateClientAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _clientService.UpdateClientAsync(clientId, updateDto, _testOwnerId);

        // Assert
        result.Should().NotBeNull();
        result.DisplayName.Should().Be("New Name");
        result.RedirectUris.Should().ContainSingle()
            .Which.Should().Be("https://example.com/new-callback");

        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

        // Verify OAuth update was called
        _oauthClientManagerMock.Verify(x => x.UpdateClientAsync(
            "test-app",
            "New Name",
            It.Is<IEnumerable<string>>(uris => uris.Contains("https://example.com/new-callback")),
            It.Is<IEnumerable<string>>(scopes => scopes.Contains("openid")),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateClientAsync_WithNonExistentClient_ShouldThrowClientNotFoundException()
    {
        // Arrange
        var clientId = Guid.NewGuid();
        var updateDto = new UpdateClientDto
        {
            DisplayName = "New Name",
            RedirectUris = new List<string> { "https://example.com/callback" }
        };

        _clientRepositoryMock
            .Setup(x => x.GetByIdAndOwnerAsync(clientId, _testOwnerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Client?)null);

        // Act
        Func<Task> act = async () => await _clientService.UpdateClientAsync(clientId, updateDto, _testOwnerId);

        // Assert
        await act.Should().ThrowAsync<ClientNotFoundException>();

        // Verify OAuth update was not called
        _oauthClientManagerMock.Verify(x => x.UpdateClientAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<IEnumerable<string>>(),
            It.IsAny<IEnumerable<string>>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateClientAsync_WithWrongOwner_ShouldThrowClientNotFoundException()
    {
        // Arrange
        var clientId = Guid.NewGuid();
        var wrongOwnerId = Guid.NewGuid();
        var updateDto = new UpdateClientDto
        {
            DisplayName = "New Name",
            RedirectUris = new List<string> { "https://example.com/callback" }
        };

        _clientRepositoryMock
            .Setup(x => x.GetByIdAndOwnerAsync(clientId, wrongOwnerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Client?)null);

        // Act
        Func<Task> act = async () => await _clientService.UpdateClientAsync(clientId, updateDto, wrongOwnerId);

        // Assert
        await act.Should().ThrowAsync<ClientNotFoundException>();

        // Verify OAuth update was not called
        _oauthClientManagerMock.Verify(x => x.UpdateClientAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<IEnumerable<string>>(),
            It.IsAny<IEnumerable<string>>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateClientAsync_WhenOAuthUpdateFails_ShouldPropagateException()
    {
        // Arrange
        var clientId = Guid.NewGuid();
        var redirectUri = RedirectUri.Create("https://example.com/callback");
        var client = Client.CreatePublic(
            "Old Name",
            "test-app",
            _testOwnerId,
            new List<RedirectUri> { redirectUri },
            new List<string> { "openid" });

        var updateDto = new UpdateClientDto
        {
            DisplayName = "New Name",
            RedirectUris = new List<string> { "https://example.com/new-callback" }
        };

        _clientRepositoryMock
            .Setup(x => x.GetByIdAndOwnerAsync(clientId, _testOwnerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(client);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _oauthClientManagerMock
            .Setup(x => x.UpdateClientAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("OAuth update failed"));

        // Act
        Func<Task> act = async () => await _clientService.UpdateClientAsync(clientId, updateDto, _testOwnerId);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("OAuth update failed");
    }

    [Fact]
    public async Task UpdateClientAsync_AddingAndRemovingRedirectUris_ShouldUpdateCorrectly()
    {
        // Arrange
        var clientId = Guid.NewGuid();
        var uri1 = RedirectUri.Create("https://example.com/callback1");
        var uri2 = RedirectUri.Create("https://example.com/callback2");
        var client = Client.CreatePublic(
            "Test App",
            "test-app",
            _testOwnerId,
            new List<RedirectUri> { uri1, uri2 },
            new List<string> { "openid" });

        var updateDto = new UpdateClientDto
        {
            DisplayName = "Test App",
            RedirectUris = new List<string>
            {
                "https://example.com/callback2",  // Keep this
                "https://example.com/callback3"   // Add new
            }
        };

        _clientRepositoryMock
            .Setup(x => x.GetByIdAndOwnerAsync(clientId, _testOwnerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(client);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _oauthClientManagerMock
            .Setup(x => x.UpdateClientAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _clientService.UpdateClientAsync(clientId, updateDto, _testOwnerId);

        // Assert
        result.RedirectUris.Should().HaveCount(2);
        result.RedirectUris.Should().Contain("https://example.com/callback2");
        result.RedirectUris.Should().Contain("https://example.com/callback3");
        result.RedirectUris.Should().NotContain("https://example.com/callback1");

        // Verify OAuth was updated with correct URIs
        _oauthClientManagerMock.Verify(x => x.UpdateClientAsync(
            "test-app",
            "Test App",
            It.Is<IEnumerable<string>>(uris =>
                uris.Contains("https://example.com/callback2") &&
                uris.Contains("https://example.com/callback3") &&
                !uris.Contains("https://example.com/callback1")),
            It.IsAny<IEnumerable<string>>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region DeleteClientAsync Tests

    [Fact]
    public async Task DeleteClientAsync_WithExistingClient_ShouldDeleteClientAndOAuthServer()
    {
        // Arrange
        var clientId = Guid.NewGuid();
        var redirectUri = RedirectUri.Create("https://example.com/callback");
        var client = Client.CreatePublic(
            "Test App",
            "test-app",
            _testOwnerId,
            new List<RedirectUri> { redirectUri },
            new List<string> { "openid" });

        _clientRepositoryMock
            .Setup(x => x.GetByIdAndOwnerAsync(clientId, _testOwnerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(client);

        _clientRepositoryMock
            .Setup(x => x.DeleteAsync(client, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _oauthClientManagerMock
            .Setup(x => x.DeleteClientAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _clientService.DeleteClientAsync(clientId, _testOwnerId);

        // Assert
        _clientRepositoryMock.Verify(x => x.DeleteAsync(client, It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

        // Verify OAuth deletion was called
        _oauthClientManagerMock.Verify(x => x.DeleteClientAsync("test-app", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteClientAsync_WithNonExistentClient_ShouldThrowClientNotFoundException()
    {
        // Arrange
        var clientId = Guid.NewGuid();

        _clientRepositoryMock
            .Setup(x => x.GetByIdAndOwnerAsync(clientId, _testOwnerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Client?)null);

        // Act
        Func<Task> act = async () => await _clientService.DeleteClientAsync(clientId, _testOwnerId);

        // Assert
        await act.Should().ThrowAsync<ClientNotFoundException>();
        _clientRepositoryMock.Verify(x => x.DeleteAsync(It.IsAny<Client>(), It.IsAny<CancellationToken>()), Times.Never);

        // Verify OAuth deletion was not called
        _oauthClientManagerMock.Verify(x => x.DeleteClientAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteClientAsync_WithWrongOwner_ShouldThrowClientNotFoundException()
    {
        // Arrange
        var clientId = Guid.NewGuid();
        var wrongOwnerId = Guid.NewGuid();

        _clientRepositoryMock
            .Setup(x => x.GetByIdAndOwnerAsync(clientId, wrongOwnerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Client?)null);

        // Act
        Func<Task> act = async () => await _clientService.DeleteClientAsync(clientId, wrongOwnerId);

        // Assert
        await act.Should().ThrowAsync<ClientNotFoundException>();
        _clientRepositoryMock.Verify(x => x.DeleteAsync(It.IsAny<Client>(), It.IsAny<CancellationToken>()), Times.Never);

        // Verify OAuth deletion was not called
        _oauthClientManagerMock.Verify(x => x.DeleteClientAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteClientAsync_WhenOAuthDeletionFails_ShouldPropagateException()
    {
        // Arrange
        var clientId = Guid.NewGuid();
        var redirectUri = RedirectUri.Create("https://example.com/callback");
        var client = Client.CreatePublic(
            "Test App",
            "test-app",
            _testOwnerId,
            new List<RedirectUri> { redirectUri },
            new List<string> { "openid" });

        _clientRepositoryMock
            .Setup(x => x.GetByIdAndOwnerAsync(clientId, _testOwnerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(client);

        _clientRepositoryMock
            .Setup(x => x.DeleteAsync(client, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _oauthClientManagerMock
            .Setup(x => x.DeleteClientAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("OAuth deletion failed"));

        // Act
        Func<Task> act = async () => await _clientService.DeleteClientAsync(clientId, _testOwnerId);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("OAuth deletion failed");
    }

    #endregion

    #region OAuth Integration Tests

    [Fact]
    public async Task RegisterClientAsync_ShouldSyncBothDomainAndOAuthStorage()
    {
        // Arrange
        var dto = new RegisterClientDto
        {
            DisplayName = "Sync Test App",
            Type = ClientType.Public,
            RedirectUris = new List<string> { "https://example.com/callback" },
            AllowedScopes = new List<string> { "openid", "profile" }
        };

        Client? capturedClient = null;

        _clientRepositoryMock
            .Setup(x => x.ExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _oauthClientManagerMock
            .Setup(x => x.ExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _clientRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Client>(), It.IsAny<CancellationToken>()))
            .Callback<Client, CancellationToken>((c, _) => capturedClient = c)
            .Returns(Task.CompletedTask);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _oauthClientManagerMock
            .Setup(x => x.RegisterClientAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _clientService.RegisterClientAsync(dto, _testOwnerId);

        // Assert
        capturedClient.Should().NotBeNull();

        // Verify both storage systems were called
        _clientRepositoryMock.Verify(x => x.AddAsync(It.IsAny<Client>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _oauthClientManagerMock.Verify(x => x.RegisterClientAsync(
            capturedClient!.ClientId,
            capturedClient.DisplayName,
            "public",
            null,
            It.IsAny<IEnumerable<string>>(),
            It.IsAny<IEnumerable<string>>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion
}