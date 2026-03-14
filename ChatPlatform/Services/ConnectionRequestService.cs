using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ChatPlatform.Api.Hubs;
using ChatPlatform.Core.DTOs;
using ChatPlatform.Core.Entities;
using ChatPlatform.Core.Interfaces;
using Microsoft.AspNetCore.SignalR;
using MongoDB.Driver;

namespace ChatPlatform.Services;

public class ConnectionRequestService : IConnectionRequestService
{
    private readonly IMongoClient _mongoClient;
    private readonly IUserService _userService;
    private readonly IHubContext<ChatHub> _hubContext;
    private readonly IPushNotificationService _pushNotificationService;

    public ConnectionRequestService(IMongoClient mongoClient, IUserService userService, IHubContext<ChatHub> hubContext, IPushNotificationService pushNotificationService)
    {
        _mongoClient = mongoClient;
        _userService = userService;
        _hubContext = hubContext;
        _pushNotificationService = pushNotificationService;
    }

    public async Task<ConnectionRequestDto> SendConnectionRequestAsync(Guid fromUserId, Guid toUserId)
    {
        var db = _mongoClient.GetDatabase("chatplatform");
        var requestsCollection = db.GetCollection<ConnectionRequest>("ConnectionRequests");
        var blocksCollection = db.GetCollection<UserBlock>("UserBlocks");

        // Check if blocked
        var isBlocked = await blocksCollection.Find(b => 
            (b.BlockerId == toUserId && b.BlockedId == fromUserId) ||
            (b.BlockerId == fromUserId && b.BlockedId == toUserId)
        ).AnyAsync();

        if (isBlocked) throw new InvalidOperationException("Connection is blocked.");

        // Check if request already exists
        var existingRequest = await requestsCollection.Find(r => 
            (r.FromUserId == fromUserId && r.ToUserId == toUserId) ||
            (r.FromUserId == toUserId && r.ToUserId == fromUserId)
        ).FirstOrDefaultAsync();

        if (existingRequest != null && existingRequest.Status != "Rejected")
        {
            throw new InvalidOperationException("A connection request already exists or is accepted.");
        }

        var request = new ConnectionRequest
        {
            Id = Guid.NewGuid(),
            FromUserId = fromUserId,
            ToUserId = toUserId,
            Status = "Pending",
            CreatedAt = DateTime.UtcNow
        };

        if (existingRequest != null)
        {
            await requestsCollection.ReplaceOneAsync(r => r.Id == existingRequest.Id, request);
        }
        else
        {
            await requestsCollection.InsertOneAsync(request);
        }

        var fromUser = await _userService.GetUserByIdAsync(fromUserId);
        var requestDto = new ConnectionRequestDto
        {
            Id = request.Id,
            FromUserId = request.FromUserId,
            FromUsername = fromUser?.Username ?? string.Empty,
            ToUserId = request.ToUserId,
            Status = request.Status,
            CreatedAt = request.CreatedAt,
            RespondedAt = request.RespondedAt
        };

        // Notify recipient via SignalR
        await _hubContext.Clients.User(toUserId.ToString()).SendAsync("receive_connection_request", requestDto);

        var usersCollection = db.GetCollection<User>("Users");
        var toUser = await usersCollection.Find(u => u.Id == toUserId).FirstOrDefaultAsync();
        if (toUser != null && toUser.PushSubscriptions.Any())
        {
            await _pushNotificationService.SendPushNotificationAsync(
                toUser.PushSubscriptions,
                "New Connection Request",
                $"{fromUser?.Username ?? "Someone"} sent you a connection request.",
                "/friends"
            );
        }

        return requestDto;
    }

    public async Task<List<ConnectionRequestDto>> GetIncomingConnectionRequestsAsync(Guid userId)
    {
        var db = _mongoClient.GetDatabase("chatplatform");
        var requestsCollection = db.GetCollection<ConnectionRequest>("ConnectionRequests");
        var usersCollection = db.GetCollection<User>("Users");

        var requests = await requestsCollection.Find(r => r.ToUserId == userId && r.Status == "Pending")
            .ToListAsync();

        var result = new List<ConnectionRequestDto>();
        foreach (var req in requests)
        {
            var fromUser = await usersCollection.Find(u => u.Id == req.FromUserId).FirstOrDefaultAsync();
            result.Add(new ConnectionRequestDto
            {
                Id = req.Id,
                FromUserId = req.FromUserId,
                FromUsername = fromUser?.Username ?? string.Empty,
                ToUserId = req.ToUserId,
                Status = req.Status,
                CreatedAt = req.CreatedAt,
                RespondedAt = req.RespondedAt
            });
        }

        return result;
    }

    public async Task<List<ConnectionRequestDto>> GetOutgoingConnectionRequestsAsync(Guid userId)
    {
        var db = _mongoClient.GetDatabase("chatplatform");
        var requestsCollection = db.GetCollection<ConnectionRequest>("ConnectionRequests");
        var usersCollection = db.GetCollection<User>("Users");

        var requests = await requestsCollection.Find(r => r.FromUserId == userId && r.Status == "Pending")
            .ToListAsync();

        var result = new List<ConnectionRequestDto>();
        foreach (var req in requests)
        {
            var fromUser = await usersCollection.Find(u => u.Id == req.FromUserId).FirstOrDefaultAsync();
            result.Add(new ConnectionRequestDto
            {
                Id = req.Id,
                FromUserId = req.FromUserId,
                FromUsername = fromUser?.Username ?? string.Empty,
                ToUserId = req.ToUserId,
                Status = req.Status,
                CreatedAt = req.CreatedAt,
                RespondedAt = req.RespondedAt
            });
        }

        return result;
    }

    public async Task<ConnectionRequestDto> RespondToConnectionRequestAsync(Guid requestId, string status)
    {
        var db = _mongoClient.GetDatabase("chatplatform");
        var requestsCollection = db.GetCollection<ConnectionRequest>("ConnectionRequests");
        var usersCollection = db.GetCollection<User>("Users");
        var blocksCollection = db.GetCollection<UserBlock>("UserBlocks");
        var reportsCollection = db.GetCollection<Report>("Reports");

        var request = await requestsCollection.Find(r => r.Id == requestId).FirstOrDefaultAsync();
        if (request == null) throw new InvalidOperationException("Connection request not found");

        request.Status = status;
        request.RespondedAt = DateTime.UtcNow;

        await requestsCollection.ReplaceOneAsync(r => r.Id == requestId, request);

        if (status == "Blocked")
        {
            await blocksCollection.InsertOneAsync(new UserBlock { BlockerId = request.ToUserId, BlockedId = request.FromUserId });
        }
        else if (status == "Reported")
        {
            await reportsCollection.InsertOneAsync(new Report 
            { 
                ReporterUserId = request.ToUserId, 
                ReportedUserId = request.FromUserId,
                Reason = "Reported during connection",
                CreatedAt = DateTime.UtcNow,
                Category = "Connection"
            });
        }

        var fromUser = await usersCollection.Find(u => u.Id == request.FromUserId).FirstOrDefaultAsync();
        return new ConnectionRequestDto
        {
            Id = request.Id,
            FromUserId = request.FromUserId,
            FromUsername = fromUser?.Username ?? string.Empty,
            ToUserId = request.ToUserId,
            Status = request.Status,
            CreatedAt = request.CreatedAt,
            RespondedAt = request.RespondedAt
        };
    }

    public async Task<List<UserDto>> GetConnectionsAsync(Guid userId)
    {
        var db = _mongoClient.GetDatabase("chatplatform");
        var requestsCollection = db.GetCollection<ConnectionRequest>("ConnectionRequests");
        var usersCollection = db.GetCollection<User>("Users");

        var requests = await requestsCollection.Find(r => 
            (r.FromUserId == userId || r.ToUserId == userId) && r.Status == "Accepted"
        ).ToListAsync();

        var result = new List<UserDto>();
        foreach (var req in requests)
        {
            var otherUserId = req.FromUserId == userId ? req.ToUserId : req.FromUserId;
            var otherUser = await usersCollection.Find(u => u.Id == otherUserId).FirstOrDefaultAsync();
            if (otherUser != null)
            {
                result.Add(new UserDto
                {
                    Id = otherUser.Id,
                    Username = otherUser.Username,
                    Avatar = otherUser.Avatar,
                    IsOnline = otherUser.IsOnline
                });
            }
        }

        return result;
    }

    public async Task<bool> AreUsersConnectedAsync(Guid userId1, Guid userId2)
    {
        var db = _mongoClient.GetDatabase("chatplatform");
        var requestsCollection = db.GetCollection<ConnectionRequest>("ConnectionRequests");

        var request = await requestsCollection.Find(r =>
            ((r.FromUserId == userId1 && r.ToUserId == userId2) ||
             (r.FromUserId == userId2 && r.ToUserId == userId1)) &&
            r.Status == "Accepted"
        ).FirstOrDefaultAsync();

        return request != null;
    }

    public async Task<string> GetConnectionStatusAsync(Guid userId, Guid targetUserId)
    {
        var db = _mongoClient.GetDatabase("chatplatform");
        
        // 1. Check if blocked
        var isBlocked = await db.GetCollection<UserBlock>("UserBlocks").Find(b => 
            (b.BlockerId == userId && b.BlockedId == targetUserId) ||
            (b.BlockerId == targetUserId && b.BlockedId == userId)
        ).AnyAsync();
        
        if (isBlocked) return "Blocked";

        // 2. Check request status
        var request = await db.GetCollection<ConnectionRequest>("ConnectionRequests").Find(r => 
            (r.FromUserId == userId && r.ToUserId == targetUserId) ||
            (r.FromUserId == targetUserId && r.ToUserId == userId)
        ).FirstOrDefaultAsync();

        if (request == null) return "NoConnection";
        return request.Status; // Pending, Accepted, Rejected
    }
}
