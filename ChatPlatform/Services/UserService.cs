using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ChatPlatform.Core.DTOs;
using ChatPlatform.Core.Entities;
using ChatPlatform.Core.Interfaces;
using MongoDB.Driver;

namespace ChatPlatform.Services;

public class UserService : IUserService
{
    private readonly IMongoClient _mongoClient;

    public UserService(IMongoClient mongoClient)
    {
        _mongoClient = mongoClient;
    }

    public async Task<UserDto?> GetUserByIdAsync(Guid userId)
    {
        var db = _mongoClient.GetDatabase("chatplatform");
        var usersCollection = db.GetCollection<Core.Entities.User>("Users");
        var user = await usersCollection.Find(u => u.Id == userId).FirstOrDefaultAsync();
        if (user == null) return null;

        return new UserDto
        {
            Id = user.Id,
            Username = user.Username,
            Avatar = user.Avatar,
            IsOnline = user.IsOnline
        };
    }

    public async Task<UserDto?> GetUserByUsernameAsync(string username)
    {
        var db = _mongoClient.GetDatabase("chatplatform");
        var usersCollection = db.GetCollection<Core.Entities.User>("Users");
        var user = await usersCollection.Find(u => u.Username.ToLower() == username.ToLower()).FirstOrDefaultAsync();
        if (user == null) return null;

        return new UserDto
        {
            Id = user.Id,
            Username = user.Username,
            Avatar = user.Avatar,
            IsOnline = user.IsOnline
        };
    }

    public async Task<List<UserDto>> SearchUsersAsync(string query, Guid currentUserId)
    {
        var db = _mongoClient.GetDatabase("chatplatform");
        var usersCollection = db.GetCollection<Core.Entities.User>("Users");
        var connRequests = db.GetCollection<ConnectionRequest>("ConnectionRequests");
        var chatRequests = db.GetCollection<ChatRequest>("ChatRequests");
        var blocks = db.GetCollection<UserBlock>("UserBlocks");

        // Infix search like Instagram
        var filter = Builders<Core.Entities.User>.Filter.Regex(u => u.Username, new MongoDB.Bson.BsonRegularExpression(query, "i"));
        var users = await usersCollection.Find(filter).Limit(10).ToListAsync();

        var result = new List<UserDto>();
        foreach (var user in users)
        {
            if (user.Id == currentUserId) continue;

            // Get Connection Status
            var connStatus = "NoConnection";
            var isBlocked = await blocks.Find(b => 
                (b.BlockerId == currentUserId && b.BlockedId == user.Id) ||
                (b.BlockerId == user.Id && b.BlockedId == currentUserId)
            ).AnyAsync();

            if (isBlocked) 
            {
                connStatus = "Blocked";
            }
            else 
            {
                var connReq = await connRequests.Find(r => 
                    (r.FromUserId == currentUserId && r.ToUserId == user.Id) ||
                    (r.FromUserId == user.Id && r.ToUserId == currentUserId)
                ).FirstOrDefaultAsync();
                
                if (connReq != null) connStatus = connReq.Status;
            }

            // Get Chat Request Status
            var chatStatus = "NoChatRequest";
            if (connStatus == "Accepted")
            {
                var chatReq = await chatRequests.Find(r => 
                    (r.FromUserId == currentUserId && r.ToUserId == user.Id) ||
                    (r.FromUserId == user.Id && r.ToUserId == currentUserId)
                ).FirstOrDefaultAsync();

                if (chatReq != null) chatStatus = chatReq.Status;
            }

            result.Add(new UserDto
            {
                Id = user.Id,
                Username = user.Username,
                Avatar = user.Avatar,
                IsOnline = user.IsOnline,
                ConnectionStatus = connStatus,
                ChatRequestStatus = chatStatus
            });
        }

        return result;
    }

    public async Task UpdateUserStatusAsync(Guid userId, bool isOnline)
    {
        var db = _mongoClient.GetDatabase("chatplatform");
        var usersCollection = db.GetCollection<Core.Entities.User>("Users");
        var update = Builders<Core.Entities.User>.Update.Set(u => u.IsOnline, isOnline);
        await usersCollection.UpdateOneAsync(u => u.Id == userId, update);
    }

    public async Task<bool> BlockUserAsync(Guid blockerId, Guid blockedId)
    {
        var db = _mongoClient.GetDatabase("chatplatform");
        var blocks = db.GetCollection<UserBlock>("UserBlocks");
        var connRequests = db.GetCollection<ConnectionRequest>("ConnectionRequests");
        var chatRequests = db.GetCollection<ChatRequest>("ChatRequests");

        // 1. Check if already blocked
        var existingBlock = await blocks.Find(b => b.BlockerId == blockerId && b.BlockedId == blockedId).FirstOrDefaultAsync();
        if (existingBlock != null) return true; // Already blocked

        // 2. Insert Block Record
        await blocks.InsertOneAsync(new UserBlock
        {
            BlockerId = blockerId,
            BlockedId = blockedId,
            CreatedAt = DateTime.UtcNow
        });

        // 3. Remove existing Connection Requests between the two
        var connFilter = Builders<ConnectionRequest>.Filter.Or(
            Builders<ConnectionRequest>.Filter.And(
                Builders<ConnectionRequest>.Filter.Eq(r => r.FromUserId, blockerId),
                Builders<ConnectionRequest>.Filter.Eq(r => r.ToUserId, blockedId)
            ),
            Builders<ConnectionRequest>.Filter.And(
                Builders<ConnectionRequest>.Filter.Eq(r => r.FromUserId, blockedId),
                Builders<ConnectionRequest>.Filter.Eq(r => r.ToUserId, blockerId)
            )
        );
        await connRequests.DeleteManyAsync(connFilter);

        // 4. Remove existing Chat Requests between the two
        var chatFilter = Builders<ChatRequest>.Filter.Or(
            Builders<ChatRequest>.Filter.And(
                Builders<ChatRequest>.Filter.Eq(r => r.FromUserId, blockerId),
                Builders<ChatRequest>.Filter.Eq(r => r.ToUserId, blockedId)
            ),
            Builders<ChatRequest>.Filter.And(
                Builders<ChatRequest>.Filter.Eq(r => r.FromUserId, blockedId),
                Builders<ChatRequest>.Filter.Eq(r => r.ToUserId, blockerId)
            )
        );
        await chatRequests.DeleteManyAsync(chatFilter);

        return true;
    }
}
