using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ChatPlatform.Core.Interfaces;
using StackExchange.Redis;

namespace ChatPlatform.Infrastructure.Redis;

public class RedisPresenceService : IPresenceService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly IDatabase _db;

    public RedisPresenceService(IConnectionMultiplexer redis)
    {
        _redis = redis;
        _db = _redis.GetDatabase();
    }

    public async Task UserConnectedAsync(Guid userId, string connectionId)
    {
        await _db.SetAddAsync($"user_connections:{userId}", connectionId);
        await _db.KeyExpireAsync($"user_connections:{userId}", TimeSpan.FromDays(1)); // Cleanup
        await _db.StringSetAsync($"connection_user:{connectionId}", userId.ToString(), TimeSpan.FromDays(1));
    }

    public async Task<Guid?> UserDisconnectedAsync(string connectionId)
    {
        var userIdStr = await _db.StringGetAsync($"connection_user:{connectionId}");
        if (userIdStr.IsNullOrEmpty) return null;

        var userId = Guid.Parse(userIdStr!);
        await _db.SetRemoveAsync($"user_connections:{userId}", connectionId);
        await _db.KeyDeleteAsync($"connection_user:{connectionId}");

        return userId;
    }

    public async Task<List<string>> GetUserConnectionsAsync(Guid userId)
    {
        var connections = await _db.SetMembersAsync($"user_connections:{userId}");
        return connections.Select(c => c.ToString()).ToList();
    }

    public async Task<bool> IsUserOnlineAsync(Guid userId)
    {
        return await _db.SetLengthAsync($"user_connections:{userId}") > 0;
    }

    public async Task<List<Guid>> GetOnlineUsersAsync(List<Guid> userIds)
    {
        var onlineUsers = new List<Guid>();
        foreach (var id in userIds)
        {
            if (await IsUserOnlineAsync(id))
            {
                onlineUsers.Add(id);
            }
        }
        return onlineUsers;
    }
}
