using System;
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
public class ConnectionController : ControllerBase
{
    private readonly IConnectionRequestService _connectionService;

    public ConnectionController(IConnectionRequestService connectionService)
    {
        _connectionService = connectionService;
    }

    [HttpPost("request")]
    public async Task<IActionResult> RequestConnection([FromBody] ConnectionRequestRequestDto dto)
    {
        try
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await _connectionService.SendConnectionRequestAsync(userId, dto.TargetUserId);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("respond")]
    public async Task<IActionResult> RespondToConnection([FromBody] ConnectionResponseDto dto)
    {
        try
        {
            // status: Accepted, Rejected, Blocked, Reported
            var result = await _connectionService.RespondToConnectionRequestAsync(dto.RequestId, dto.Status);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("incoming")]
    public async Task<IActionResult> GetIncoming()
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var requests = await _connectionService.GetIncomingConnectionRequestsAsync(userId);
        return Ok(requests);
    }

    [HttpGet("connections")]
    public async Task<IActionResult> GetConnections()
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var connections = await _connectionService.GetConnectionsAsync(userId);
        return Ok(connections);
    }

    [HttpGet("status/{targetUserId}")]
    public async Task<IActionResult> GetStatus(Guid targetUserId)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var status = await _connectionService.GetConnectionStatusAsync(userId, targetUserId);
        return Ok(new { status });
    }
}

public class ConnectionRequestRequestDto
{
    public Guid TargetUserId { get; set; }
}

public class ConnectionResponseDto
{
    public Guid RequestId { get; set; }
    public string Status { get; set; } = string.Empty; // Accepted, Rejected, Blocked, Reported
}
