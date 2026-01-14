using GateKeeper.Application.Clients.DTOs;
using GateKeeper.Application.Clients.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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
    /// Get all OAuth clients
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int skip = 0, [FromQuery] int take = 50)
    {
        var clients = await _clientService.GetAllClientsAsync(skip, take);
        return Ok(clients);
    }

    /// <summary>
    /// Get OAuth client by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var client = await _clientService.GetClientByIdAsync(id);
        return Ok(client);
    }

    /// <summary>
    /// Register a new OAuth client
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Register([FromBody] RegisterClientDto dto)
    {
        var client = await _clientService.RegisterClientAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = client.Id }, client);
    }

    /// <summary>
    /// Update existing OAuth client
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateClientDto dto)
    {
        var client = await _clientService.UpdateClientAsync(id, dto);
        return Ok(client);
    }

    /// <summary>
    /// Delete OAuth client
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _clientService.DeleteClientAsync(id);
        return NoContent();
    }
}
