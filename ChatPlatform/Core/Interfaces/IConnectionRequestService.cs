using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ChatPlatform.Core.DTOs;

namespace ChatPlatform.Core.Interfaces;

public interface IConnectionRequestService
{
    Task<ConnectionRequestDto> SendConnectionRequestAsync(Guid fromUserId, Guid toUserId);
    Task<List<ConnectionRequestDto>> GetIncomingConnectionRequestsAsync(Guid userId);
    Task<List<ConnectionRequestDto>> GetOutgoingConnectionRequestsAsync(Guid userId);
    Task<ConnectionRequestDto> RespondToConnectionRequestAsync(Guid requestId, string status);
    Task<List<UserDto>> GetConnectionsAsync(Guid userId);
    Task<bool> AreUsersConnectedAsync(Guid userId1, Guid userId2);
    Task<string> GetConnectionStatusAsync(Guid userId, Guid targetUserId);
}
