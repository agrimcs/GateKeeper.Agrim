using GateKeeper.Application.Users.DTOs;
using GateKeeper.Application.Users.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GateKeeper.Server.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthenticationController : ControllerBase
{
    private readonly UserService _userService;

    public AuthenticationController(UserService userService)
    {
        _userService = userService;
    }

    /// <summary>
    /// Register a new user account
    /// </summary>
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterUserDto dto)
    {
        var user = await _userService.RegisterAsync(dto);
        return Ok(user);
    }

    /// <summary>
    /// Login with email and password
    /// </summary>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginUserDto dto)
    {
        var user = await _userService.LoginAsync(dto);
        return Ok(user);
    }

    /// <summary>
    /// Get current user profile
    /// </summary>
    [HttpGet("profile/{id}")]
    [Authorize]
    public async Task<IActionResult> GetProfile(Guid id)
    {
        var profile = await _userService.GetProfileAsync(id);
        return Ok(profile);
    }
}
