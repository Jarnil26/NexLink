using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ChatPlatform.Api.Hubs;
using ChatPlatform.Core.DTOs;
using ChatPlatform.Core.Entities;
using ChatPlatform.Core.Interfaces;
using Microsoft.AspNetCore.SignalR;
using MongoDB.Driver;

namespace ChatPlatform.Services;

public class ChatRequestService : IChatRequestService
{
    private readonly IMongoClient _mongoClient;
    private readonly IUserService _userService;
    private readonly IConnectionRequestService _connectionService;
    private readonly IChatService _chatService;
    private readonly IHubContext<ChatHub> _hubContext;

    public ChatRequestService(IMongoClient mongoClient, IUserService userService, IConnectionRequestService connectionService, IChatService chatService, IHubContext<ChatHub> hubContext)
    {
        _mongoClient = mongoClient;
        _userService = userService;
        _connectionService = connectionService;
        _chatService = chatService;
        _hubContext = hubContext;
    }

    public async Task<ChatRequestDto> SendChatRequestAsync(Guid fromUserId, Guid toUserId)
    {
        var db = _mongoClient.GetDatabase("chatplatform");
        var chatRequestsCollection = db.GetCollection<ChatRequest>("ChatRequests");
        var blocksCollection = db.GetCollection<UserBlock>("UserBlocks");

        // 1. Check if blocked
        var isBlocked = await blocksCollection.Find(b => 
            (b.BlockerId == toUserId && b.BlockedId == fromUserId) ||
            (b.BlockerId == fromUserId && b.BlockedId == toUserId)
        ).AnyAsync();

        if (isBlocked) throw new InvalidOperationException("This connection is blocked.");

        // 2. Check if Connection is Accepted
        var isConnected = await _connectionService.AreUsersConnectedAsync(fromUserId, toUserId);
        if (!isConnected) throw new InvalidOperationException("Users must be connected before sending a chat request.");

        // 3. Find ConnectionId
        var connectionRequest = await db.GetCollection<ConnectionRequest>("ConnectionRequests").Find(r => 
            ((r.FromUserId == fromUserId && r.ToUserId == toUserId) || (r.FromUserId == toUserId && r.ToUserId == fromUserId)) && r.Status == "Accepted"
        ).FirstOrDefaultAsync();

        // 4. Check if chat request already exists
        var existingRequest = await chatRequestsCollection.Find(r =>
            r.FromUserId == fromUserId && r.ToUserId == toUserId && (r.Status == "Pending" || r.Status == "Accepted")
        ).FirstOrDefaultAsync();

        if (existingRequest != null) 
        {
            if (existingRequest.Status == "Accepted") throw new InvalidOperationException("Chat is already enabled.");
            throw new InvalidOperationException("A chat request is already pending.");
        }

        var request = new ChatRequest
        {
            Id = Guid.NewGuid(),
            FromUserId = fromUserId,
            ToUserId = toUserId,
            ConnectionId = connectionRequest.Id,
            ChatId = null,
            Status = "Pending",
            CreatedAt = DateTime.UtcNow
        };

        await chatRequestsCollection.InsertOneAsync(request);

        var fromUser = await _userService.GetUserByIdAsync(fromUserId);
        var requestDto = new ChatRequestDto
        {
            Id = request.Id,
            FromUserId = request.FromUserId,
            FromUsername = fromUser?.Username ?? string.Empty,
            ToUserId = request.ToUserId,
            ChatId = request.ChatId ?? Guid.Empty,
            Status = request.Status,
            CreatedAt = request.CreatedAt,
            RespondedAt = request.RespondedAt
        };

        // Notify recipient via SignalR
        await _hubContext.Clients.User(toUserId.ToString()).SendAsync("receive_chat_request", requestDto);

        return requestDto;
    }

    public async Task<List<ChatRequestDto>> GetIncomingChatRequestsAsync(Guid userId)
    {
        var db = _mongoClient.GetDatabase("chatplatform");
        var chatRequestsCollection = db.GetCollection<ChatRequest>("ChatRequests");
        var usersCollection = db.GetCollection<User>("Users");

        var requests = await chatRequestsCollection.Find(r => r.ToUserId == userId && r.Status == "Pending")
            .ToListAsync();

        var result = new List<ChatRequestDto>();
        foreach (var req in requests)
        {
            var fromUser = await usersCollection.Find(u => u.Id == req.FromUserId).FirstOrDefaultAsync();
            result.Add(new ChatRequestDto
            {
                Id = req.Id,
                FromUserId = req.FromUserId,
                FromUsername = fromUser?.Username ?? string.Empty,
                ToUserId = req.ToUserId,
                ChatId = req.ChatId ?? Guid.Empty,
                Status = req.Status,
                CreatedAt = req.CreatedAt,
                RespondedAt = req.RespondedAt
            });
        }

        return result;
    }

    public async Task<List<ChatRequestDto>> GetOutgoingChatRequestsAsync(Guid userId)
    {
        var db = _mongoClient.GetDatabase("chatplatform");
        var chatRequestsCollection = db.GetCollection<ChatRequest>("ChatRequests");
        var usersCollection = db.GetCollection<User>("Users");

        var requests = await chatRequestsCollection.Find(r => r.FromUserId == userId && r.Status == "Pending")
            .ToListAsync();

        var result = new List<ChatRequestDto>();
        foreach (var req in requests)
        {
            var fromUser = await usersCollection.Find(u => u.Id == req.FromUserId).FirstOrDefaultAsync();
            result.Add(new ChatRequestDto
            {
                Id = req.Id,
                FromUserId = req.FromUserId,
                FromUsername = fromUser?.Username ?? string.Empty,
                ToUserId = req.ToUserId,
                ChatId = req.ChatId ?? Guid.Empty,
                Status = req.Status,
                CreatedAt = req.CreatedAt,
                RespondedAt = req.RespondedAt
            });
        }

        return result;
    }

    public async Task<ChatRequestDto> RespondToChatRequestAsync(Guid requestId, string status)
    {
        var db = _mongoClient.GetDatabase("chatplatform");
        var chatRequestsCollection = db.GetCollection<ChatRequest>("ChatRequests");
        var usersCollection = db.GetCollection<User>("Users");
        var blocksCollection = db.GetCollection<UserBlock>("UserBlocks");
        var reportsCollection = db.GetCollection<Report>("Reports");

        var request = await chatRequestsCollection.Find(r => r.Id == requestId).FirstOrDefaultAsync();
        if (request == null) throw new InvalidOperationException("Chat request not found");

        request.Status = status;
        request.RespondedAt = DateTime.UtcNow;

        await chatRequestsCollection.ReplaceOneAsync(r => r.Id == requestId, request);

        if (status == "Accepted")
        {
            // CREATE THE CHAT ROOM HERE
            var chat = await _chatService.CreateOrGetOneToOneChatAsync(request.FromUserId, request.ToUserId);
            request.ChatId = chat.Id;
        }
        else if (status == "Blocked")
        {
            await blocksCollection.InsertOneAsync(new UserBlock { BlockerId = request.ToUserId, BlockedId = request.FromUserId });
        }
        else if (status == "Reported")
        {
            await reportsCollection.InsertOneAsync(new Report 
            { 
                ReporterUserId = request.ToUserId, 
                ReportedUserId = request.FromUserId,
                Reason = "Reported from chat request",
                CreatedAt = DateTime.UtcNow
            });
        }

        var fromUser = await usersCollection.Find(u => u.Id == request.FromUserId).FirstOrDefaultAsync();
        return new ChatRequestDto
        {
            Id = request.Id,
            FromUserId = request.FromUserId,
            FromUsername = fromUser?.Username ?? string.Empty,
            ToUserId = request.ToUserId,
            ChatId = request.ChatId ?? Guid.Empty,
            Status = request.Status,
            CreatedAt = request.CreatedAt,
            RespondedAt = request.RespondedAt
        };
    }

    public async Task<bool> CanUsersChatAsync(Guid userId1, Guid userId2)
    {
        var db = _mongoClient.GetDatabase("chatplatform");
        var chatRequestsCollection = db.GetCollection<ChatRequest>("ChatRequests");

        var request = await chatRequestsCollection.Find(r =>
            ((r.FromUserId == userId1 && r.ToUserId == userId2) ||
             (r.FromUserId == userId2 && r.ToUserId == userId1)) &&
            r.Status == "Accepted"
        ).FirstOrDefaultAsync();

        return request != null;
    }

    public async Task<string> GetChatRequestStatusAsync(Guid userId, Guid targetUserId)
    {
        var db = _mongoClient.GetDatabase("chatplatform");
        
        // Block check is already done by Connection status, but for safety:
        var isBlocked = await db.GetCollection<UserBlock>("UserBlocks").Find(b => 
            (b.BlockerId == userId && b.BlockedId == targetUserId) ||
            (b.BlockerId == targetUserId && b.BlockedId == userId)
        ).AnyAsync();
        
        if (isBlocked) return "Blocked";

        var request = await db.GetCollection<ChatRequest>("ChatRequests").Find(r => 
            (r.FromUserId == userId && r.ToUserId == targetUserId) ||
            (r.FromUserId == targetUserId && r.ToUserId == userId)
        ).FirstOrDefaultAsync();

        if (request == null) return "NoChatRequest";
        return request.Status;
    }
}
