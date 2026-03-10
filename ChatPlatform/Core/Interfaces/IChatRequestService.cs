using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ChatPlatform.Core.DTOs;

namespace ChatPlatform.Core.Interfaces;

public interface IChatRequestService
{
    Task<ChatRequestDto> SendChatRequestAsync(Guid fromUserId, Guid toUserId);
    Task<List<ChatRequestDto>> GetIncomingChatRequestsAsync(Guid userId);
    Task<List<ChatRequestDto>> GetOutgoingChatRequestsAsync(Guid userId);
    Task<ChatRequestDto> RespondToChatRequestAsync(Guid requestId, string status);
    Task<bool> CanUsersChatAsync(Guid userId1, Guid userId2);
    Task<string> GetChatRequestStatusAsync(Guid userId, Guid targetUserId);
}
