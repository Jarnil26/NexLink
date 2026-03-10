using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using ChatPlatform.Core.DTOs;
using ChatPlatform.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace ChatPlatform.Api.Hubs;

[Authorize]
public class ChatHub : Hub
{
    private readonly IPresenceService _presenceService;
    private readonly IMessageService _messageService;
    private readonly IChatService _chatService;
    private readonly IChatRequestService _chatRequestService;
    private readonly IUserService _userService;

    public ChatHub(IPresenceService presenceService, IMessageService messageService, IChatService chatService, IChatRequestService chatRequestService, IUserService userService)
    {
        _presenceService = presenceService;
        _messageService = messageService;
        _chatService = chatService;
        _chatRequestService = chatRequestService;
        _userService = userService;
    }

    public override async Task OnConnectedAsync()
    {
        try 
        {
            var userIdStr = Context.User!.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdStr)) return;

            var userId = Guid.Parse(userIdStr);
            await _presenceService.UserConnectedAsync(userId, Context.ConnectionId);
            await _userService.UpdateUserStatusAsync(userId, true);

            await Clients.Others.SendAsync("user_online", userId);

            // Fetch chats and join groups
            var chats = await _chatService.GetUserChatsAsync(userId);
            foreach (var chat in chats)
            {
                // In the new flow, a chat exists ONLY if chat request was accepted.
                // However, we still check permission just in case.
                var otherUser = chat.Participants.FirstOrDefault(p => p.Id != userId);
                if (otherUser != null)
                {
                    var canChat = await _chatRequestService.CanUsersChatAsync(userId, otherUser.Id);
                    if (canChat)
                    {
                        await Groups.AddToGroupAsync(Context.ConnectionId, chat.Id.ToString());
                    }
                }
            }

            await base.OnConnectedAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ChatHub] OnConnected error: {ex.Message}");
            throw;
        }
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = await _presenceService.UserDisconnectedAsync(Context.ConnectionId);

        if (userId.HasValue)
        {
            var isOnline = await _presenceService.IsUserOnlineAsync(userId.Value);
            if (!isOnline)
            {
                await _userService.UpdateUserStatusAsync(userId.Value, false);
                await Clients.Others.SendAsync("user_offline", userId.Value);
            }
        }

        await base.OnDisconnectedAsync(exception);
    }

    public async Task SendMessage(SendMessageDto messageDto)
    {
        try 
        {
            var userId = Guid.Parse(Context.User!.FindFirstValue(ClaimTypes.NameIdentifier)!);
            
            // Explicitly check chat permission status before letting the message through
            var chats = await _chatService.GetUserChatsAsync(userId);
            var chat = chats.FirstOrDefault(c => c.Id == messageDto.ChatId);
            var otherUser = chat?.Participants.FirstOrDefault(p => p.Id != userId);
            
            if (otherUser == null) throw new HubException("Chat not found or no other participant.");

            var canChat = await _chatRequestService.CanUsersChatAsync(userId, otherUser.Id);
            if (!canChat) throw new HubException("Chat is not enabled yet. Waiting for approval.");

            Console.WriteLine($"[ChatHub] User {userId} sending message to chat {messageDto.ChatId}");
            
            var message = await _messageService.SendMessageAsync(userId, messageDto);
            await Clients.Group(messageDto.ChatId.ToString()).SendAsync("receive_message", message);
            Console.WriteLine("[ChatHub] Message broadcasted successfully");
        }
        catch (UnauthorizedAccessException ex)
        {
            Console.WriteLine($"[ChatHub] Unauthorized: {ex.Message}");
            throw new HubException(ex.Message);
        }
        catch (HubException) { throw; }
        catch (Exception ex)
        {
            Console.WriteLine($"[ChatHub] SendMessage error: {ex.GetType().Name} - {ex.Message}");
            throw new HubException("An error occurred while sending your message.");
        }
    }

    public async Task Typing(Guid chatId)
    {
        var userId = Guid.Parse(Context.User!.FindFirstValue(ClaimTypes.NameIdentifier)!);
        await Clients.Group(chatId.ToString()).SendAsync("user_typing", new { ChatId = chatId, UserId = userId });
    }

    public async Task JoinChat(string chatId)
    {
        try 
        {
             var userId = Guid.Parse(Context.User!.FindFirstValue(ClaimTypes.NameIdentifier)!);
             var chatGuid = Guid.Parse(chatId);

             // Security check: only allow joining if permission exists
             var chats = await _chatService.GetUserChatsAsync(userId);
             var chat = chats.FirstOrDefault(c => c.Id == chatGuid);
             var otherUser = chat?.Participants.FirstOrDefault(p => p.Id != userId);

             if (otherUser != null)
             {
                 var canChat = await _chatRequestService.CanUsersChatAsync(userId, otherUser.Id);
                 if (!canChat) throw new HubException("Chat is not enabled.");
             }

             Console.WriteLine($"[ChatHub] Connection {Context.ConnectionId} joining group {chatId}");
             await Groups.AddToGroupAsync(Context.ConnectionId, chatId);
        }
        catch (Exception ex)
        {
             Console.WriteLine($"[ChatHub] JoinChat error: {ex.Message}");
             throw new HubException("Failed to join chat group.");
        }
    }

    public async Task MarkAsRead(Guid chatId)
    {
        var userId = Guid.Parse(Context.User!.FindFirstValue(ClaimTypes.NameIdentifier)!);
        await _messageService.MarkAsReadAsync(chatId, userId);
        var readAt = DateTime.UtcNow;
        await Clients.Group(chatId.ToString()).SendAsync("message_read", new { ChatId = chatId, UserId = userId, ReadAt = readAt });
    }
}
