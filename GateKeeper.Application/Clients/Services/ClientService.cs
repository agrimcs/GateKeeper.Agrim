using GateKeeper.Application.Clients.DTOs;
using GateKeeper.Domain.Entities;
using GateKeeper.Domain.Enums;
using GateKeeper.Domain.Exceptions;
using GateKeeper.Domain.Interfaces;
using GateKeeper.Domain.ValueObjects;

namespace GateKeeper.Application.Clients.Services;

/// <summary>
/// Application service for OAuth client management.
/// Handles client registration, configuration, and secret management.
/// </summary>
public class ClientService
{
    private readonly IClientRepository _clientRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IUnitOfWork _unitOfWork;

    public ClientService(
        IClientRepository clientRepository,
        IPasswordHasher passwordHasher,
        IUnitOfWork unitOfWork)
    {
        _clientRepository = clientRepository;
        _passwordHasher = passwordHasher;
        _unitOfWork = unitOfWork;
    }

    /// <summary>
    /// Registers a new OAuth client.
    /// For confidential clients, returns plain-text secret that must be saved by user.
    /// </summary>
    public async Task<ClientResponseDto> RegisterClientAsync(
        RegisterClientDto dto,
        CancellationToken cancellationToken = default)
    {
        // Generate unique client ID
        var clientId = GenerateClientId(dto.DisplayName);

        // Check if client ID already exists
        if (await _clientRepository.ExistsAsync(clientId, cancellationToken))
        {
            clientId = $"{clientId}-{Guid.NewGuid().ToString()[..8]}";
        }

        // Parse redirect URIs to value objects (validates HTTPS requirement)
        var redirectUris = dto.RedirectUris
            .Select(uri => RedirectUri.Create(uri))
            .ToList();

        Client client;
        string? plainTextSecret = null;

        if (dto.Type == ClientType.Confidential)
        {
            // Generate secret for confidential clients
            var secret = ClientSecret.Generate();
            plainTextSecret = secret.HashedValue; // Store plain text to return to user
            
            // Hash the secret before storing
            var hashedSecret = ClientSecret.FromHashed(_passwordHasher.HashPassword(plainTextSecret));

            client = Client.CreateConfidential(
                dto.DisplayName,
                clientId,
                hashedSecret,
                redirectUris,
                dto.AllowedScopes);
        }
        else
        {
            // Public clients don't have secrets
            client = Client.CreatePublic(
                dto.DisplayName,
                clientId,
                redirectUris,
                dto.AllowedScopes);
        }

        await _clientRepository.AddAsync(client, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MapToResponseDto(client, plainTextSecret);
    }

    /// <summary>
    /// Gets a client by ID.
    /// </summary>
    public async Task<ClientResponseDto> GetClientByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var client = await _clientRepository.GetByIdAsync(id, cancellationToken);
        if (client == null)
        {
            throw new ClientNotFoundException(id);
        }

        return MapToResponseDto(client);
    }

    /// <summary>
    /// Gets a client by client ID string.
    /// </summary>
    public async Task<ClientResponseDto> GetClientByClientIdAsync(
        string clientId,
        CancellationToken cancellationToken = default)
    {
        var client = await _clientRepository.GetByClientIdAsync(clientId, cancellationToken);
        if (client == null)
        {
            throw new ClientNotFoundException(clientId);
        }

        return MapToResponseDto(client);
    }

    /// <summary>
    /// Gets all registered clients with pagination.
    /// </summary>
    public async Task<List<ClientResponseDto>> GetAllClientsAsync(
        int skip = 0,
        int take = 50,
        CancellationToken cancellationToken = default)
    {
        var clients = await _clientRepository.GetAllAsync(skip, take, cancellationToken);
        return clients.Select(c => MapToResponseDto(c)).ToList();
    }

    /// <summary>
    /// Updates client configuration.
    /// </summary>
    public async Task<ClientResponseDto> UpdateClientAsync(
        Guid id,
        UpdateClientDto dto,
        CancellationToken cancellationToken = default)
    {
        var client = await _clientRepository.GetByIdAsync(id, cancellationToken);
        if (client == null)
        {
            throw new ClientNotFoundException(id);
        }

        // Update display name
        client.UpdateDisplayName(dto.DisplayName);

        // Update redirect URIs - remove old ones and add new ones
        var currentUris = client.RedirectUris.ToList();
        var newUris = dto.RedirectUris.Select(uri => RedirectUri.Create(uri)).ToList();

        // Remove URIs that are no longer in the list
        foreach (var uri in currentUris)
        {
            if (!newUris.Any(u => u.Value == uri.Value))
            {
                client.RemoveRedirectUri(uri);
            }
        }

        // Add new URIs that aren't already present
        foreach (var uri in newUris)
        {
            if (!currentUris.Any(u => u.Value == uri.Value))
            {
                client.AddRedirectUri(uri);
            }
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MapToResponseDto(client);
    }

    /// <summary>
    /// Deletes a client.
    /// </summary>
    public async Task DeleteClientAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var client = await _clientRepository.GetByIdAsync(id, cancellationToken);
        if (client == null)
        {
            throw new ClientNotFoundException(id);
        }

        await _clientRepository.DeleteAsync(client, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private static string GenerateClientId(string displayName)
    {
        // Create client ID from display name: "My App" -> "my-app"
        var clientId = displayName
            .ToLowerInvariant()
            .Replace(" ", "-")
            .Replace("_", "-");

        // Remove any non-alphanumeric characters except hyphens
        clientId = new string(clientId.Where(c => char.IsLetterOrDigit(c) || c == '-').ToArray());

        return clientId;
    }

    private static ClientResponseDto MapToResponseDto(Client client, string? plainTextSecret = null)
    {
        return new ClientResponseDto
        {
            Id = client.Id,
            ClientId = client.ClientId,
            DisplayName = client.DisplayName,
            Type = client.Type,
            RedirectUris = client.RedirectUris.Select(u => u.Value).ToList(),
            AllowedScopes = client.AllowedScopes.ToList(),
            CreatedAt = client.CreatedAt,
            PlainTextSecret = plainTextSecret // Only set on creation
        };
    }
}
