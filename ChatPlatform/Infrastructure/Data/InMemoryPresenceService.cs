using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ChatPlatform.Core.Interfaces;

namespace ChatPlatform.Infrastructure.Data;

public class InMemoryPresenceService : IPresenceService
{
    private static readonly ConcurrentDictionary<Guid, HashSet<string>> UserConnections = new();
    private static readonly ConcurrentDictionary<string, Guid> ConnectionUsers = new();

    public Task UserConnectedAsync(Guid userId, string connectionId)
    {
        var connections = UserConnections.GetOrAdd(userId, _ => new HashSet<string>());
        lock (connections)
        {
            connections.Add(connectionId);
        }
        ConnectionUsers[connectionId] = userId;
        return Task.CompletedTask;
    }

    public Task<Guid?> UserDisconnectedAsync(string connectionId)
    {
        if (ConnectionUsers.TryRemove(connectionId, out var userId))
        {
            if (UserConnections.TryGetValue(userId, out var connections))
            {
                lock (connections)
                {
                    connections.Remove(connectionId);
                    if (connections.Count == 0)
                    {
                        UserConnections.TryRemove(userId, out _);
                    }
                }
            }
            return Task.FromResult<Guid?>(userId);
        }
        return Task.FromResult<Guid?>(null);
    }

    public Task<List<string>> GetUserConnectionsAsync(Guid userId)
    {
        if (UserConnections.TryGetValue(userId, out var connections))
        {
            lock (connections)
            {
                return Task.FromResult(connections.ToList());
            }
        }
        return Task.FromResult(new List<string>());
    }

    public Task<bool> IsUserOnlineAsync(Guid userId)
    {
        return Task.FromResult(UserConnections.ContainsKey(userId));
    }

    public Task<List<Guid>> GetOnlineUsersAsync(List<Guid> userIds)
    {
        var onlineUsers = userIds.Where(id => UserConnections.ContainsKey(id)).ToList();
        return Task.FromResult(onlineUsers);
    }
}
