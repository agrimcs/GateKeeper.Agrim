using GateKeeper.Application.Users.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GateKeeper.Server.Controllers;

[ApiController]
[Route("api/users")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly UserService _userService;

    public UsersController(UserService userService)
    {
        _userService = userService;
    }

    /// <summary>
    /// Get all users (admin endpoint)
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int skip = 0, [FromQuery] int take = 50)
    {
        var users = await _userService.GetAllUsersAsync(skip, take);
        return Ok(users);
    }

    /// <summary>
    /// Get user by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var user = await _userService.GetProfileAsync(id);
        return Ok(user);
    }
}
