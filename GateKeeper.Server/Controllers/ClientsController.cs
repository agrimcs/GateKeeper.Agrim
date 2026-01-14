using GateKeeper.Application.Clients.DTOs;
using GateKeeper.Application.Clients.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace GateKeeper.Server.Controllers;

[ApiController]
[Route("api/clients")]
[Authorize]
public class ClientsController : ControllerBase
{
    private readonly ClientService _clientService;

    public ClientsController(ClientService clientService)
    {
        _clientService = clientService;
    }

    /// <summary>
    /// Helper to get current user ID from JWT claims
    /// </summary>
    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst("sub") ?? User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
        {
            throw new UnauthorizedAccessException("User ID not found in token");
        }
        return userId;
    }

    /// <summary>
    /// Get all OAuth clients
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int skip = 0, [FromQuery] int take = 50)
    {
        var ownerId = GetCurrentUserId();
        var clients = await _clientService.GetAllClientsAsync(ownerId, skip, take);
        return Ok(clients);
    }

    /// <summary>
    /// Get OAuth client by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var ownerId = GetCurrentUserId();
        var client = await _clientService.GetClientByIdAsync(id, ownerId);
        return Ok(client);
    }

    /// <summary>
    /// Register a new OAuth client
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Register([FromBody] RegisterClientDto dto)
    {
        var ownerId = GetCurrentUserId();
        var client = await _clientService.RegisterClientAsync(dto, ownerId);
        return CreatedAtAction(nameof(GetById), new { id = client.Id }, client);
    }

    /// <summary>
    /// Update existing OAuth client
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateClientDto dto)
    {
        var ownerId = GetCurrentUserId();
        var client = await _clientService.UpdateClientAsync(id, dto, ownerId);
        return Ok(client);
    }

    /// <summary>
    /// Delete OAuth client
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var ownerId = GetCurrentUserId();
        await _clientService.DeleteClientAsync(id, ownerId);
        return NoContent();
    }
}
