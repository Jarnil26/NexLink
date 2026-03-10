using System;
using System.Security.Claims;
using System.Threading.Tasks;
using ChatPlatform.Core.DTOs;
using ChatPlatform.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using Microsoft.AspNetCore.SignalR;
using ChatPlatform.Api.Hubs;

namespace ChatPlatform.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ChatController : ControllerBase
{
    private readonly IChatService _chatService;
    private readonly IMessageService _messageService;
    private readonly IHubContext<ChatHub> _hubContext;

    public ChatController(IChatService chatService, IMessageService messageService, IHubContext<ChatHub> hubContext)
    {
        _chatService = chatService;
        _messageService = messageService;
        _hubContext = hubContext;
    }

    [HttpGet("rooms")]
    public async Task<IActionResult> GetChatRooms()
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        // This should only return chats where ChatRequest is Accepted.
        // In the new flow, CreateOrGetOneToOneChatAsync is only called when ChatRequest is Accepted.
        var chats = await _chatService.GetUserChatsAsync(userId);
        return Ok(chats);
    }

    [HttpGet("{chatId}/messages")]
    public async Task<IActionResult> GetChatMessages(Guid chatId, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        try
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var messages = await _messageService.GetChatMessagesAsync(chatId, userId, page, pageSize);
            return Ok(messages);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
    }

    [HttpPost("send")]
    public async Task<IActionResult> SendMessage([FromBody] SendMessageDto dto)
    {
        try
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await _messageService.SendMessageAsync(userId, dto);
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("mark-seen/{chatId}")]
    public async Task<IActionResult> MarkAsSeen(Guid chatId)
    {
        try
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            await _messageService.MarkAsReadAsync(chatId, userId);
            
            var readAt = DateTime.UtcNow;
            // Broadcast to the group that messages were read
            await _hubContext.Clients.Group(chatId.ToString()).SendAsync("message_read", new { ChatId = chatId, UserId = userId, ReadAt = readAt });

            return Ok(new { message = "Messages marked as seen.", readAt = readAt });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
