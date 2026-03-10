using System;
using System.Security.Claims;
using System.Threading.Tasks;
using ChatPlatform.Core.DTOs;
using ChatPlatform.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ChatPlatform.Api.Controllers;

[ApiController]
[Route("api/chat-request")]
[Authorize]
public class ChatRequestController : ControllerBase
{
    private readonly IChatRequestService _chatRequestService;

    public ChatRequestController(IChatRequestService chatRequestService)
    {
        _chatRequestService = chatRequestService;
    }

    [HttpPost("request")]
    public async Task<IActionResult> RequestChat([FromBody] ChatRequestRequestDto dto)
    {
        try
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await _chatRequestService.SendChatRequestAsync(userId, dto.TargetUserId);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("respond")]
    public async Task<IActionResult> RespondToChat([FromBody] ChatResponseDto dto)
    {
        try
        {
            // status: Accepted, Rejected, Blocked, Reported
            var result = await _chatRequestService.RespondToChatRequestAsync(dto.RequestId, dto.Status);
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
        var requests = await _chatRequestService.GetIncomingChatRequestsAsync(userId);
        return Ok(requests);
    }

    [HttpGet("status/{targetUserId}")]
    public async Task<IActionResult> GetStatus(Guid targetUserId)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var status = await _chatRequestService.GetChatRequestStatusAsync(userId, targetUserId);
        return Ok(new { status });
    }
}

public class ChatRequestRequestDto
{
    public Guid TargetUserId { get; set; }
}

public class ChatResponseDto
{
    public Guid RequestId { get; set; }
    public string Status { get; set; } = string.Empty; // Accepted, Rejected, Blocked, Reported
}
