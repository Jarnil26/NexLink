using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ChatPlatform.Core.DTOs;

namespace ChatPlatform.Core.Interfaces;

public interface IChatService
{
    Task<ChatDto> CreateOrGetOneToOneChatAsync(Guid currentUserId, Guid otherUserId);
    Task<List<ChatDto>> GetUserChatsAsync(Guid userId);
    Task<bool> IsUserInChatAsync(Guid userId, Guid chatId);
}
