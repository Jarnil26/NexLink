using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using ChatPlatform.Core.DTOs;
using ChatPlatform.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ChatPlatform.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UserController : ControllerBase
{
    private readonly IUserService _userService;

    public UserController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetCurrentUser()
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var user = await _userService.GetUserByIdAsync(userId);
        
        if (user == null) return NotFound();
        return Ok(user);
    }

    [HttpGet("id/{id}")]
    public async Task<IActionResult> GetUserById(Guid id)
    {
        var user = await _userService.GetUserByIdAsync(id);
        if (user == null) return NotFound();
        return Ok(user);
    }

    [HttpGet("search")]
    public async Task<IActionResult> SearchUsers([FromQuery] string q)
    {
        if (string.IsNullOrEmpty(q)) return Ok(new List<UserDto>());
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var users = await _userService.SearchUsersAsync(q, userId);
        return Ok(users);
    }

    [HttpPost("{id}/block")]
    public async Task<IActionResult> BlockUser(Guid id)
    {
        var blockerId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        
        if (blockerId == id)
            return BadRequest("You cannot block yourself.");

        var result = await _userService.BlockUserAsync(blockerId, id);
        if (result)
        {
            return Ok(new { message = "User blocked successfully." });
        }
        
        return BadRequest("Failed to block user.");
    }
}
