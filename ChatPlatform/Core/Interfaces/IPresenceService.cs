using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ChatPlatform.Core.Interfaces;

public interface IPresenceService
{
    Task UserConnectedAsync(Guid userId, string connectionId);
    Task<Guid?> UserDisconnectedAsync(string connectionId);
    Task<List<string>> GetUserConnectionsAsync(Guid userId);
    Task<bool> IsUserOnlineAsync(Guid userId);
    Task<List<Guid>> GetOnlineUsersAsync(List<Guid> userIds);
}
