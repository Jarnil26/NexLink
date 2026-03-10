using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ChatPlatform.Core.DTOs;

namespace ChatPlatform.Core.Interfaces;

public interface IMessageService
{
    Task<MessageDto> SendMessageAsync(Guid senderId, SendMessageDto sendMessageDto);
    Task<List<MessageDto>> GetChatMessagesAsync(Guid chatId, Guid userId, int page = 1, int pageSize = 50);
    Task MarkAsReadAsync(Guid chatId, Guid userId);
}
