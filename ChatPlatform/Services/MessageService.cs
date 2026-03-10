using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ChatPlatform.Core.DTOs;
using ChatPlatform.Core.Entities;
using ChatPlatform.Core.Interfaces;
using MongoDB.Driver;

namespace ChatPlatform.Services;

public class MessageService : IMessageService
{
    private readonly IMongoClient _mongoClient;
    private readonly IChatRequestService _chatRequestService;

    public MessageService(IMongoClient mongoClient, IChatRequestService chatRequestService)
    {
        _mongoClient = mongoClient;
        _chatRequestService = chatRequestService;
    }

    public async Task<MessageDto> SendMessageAsync(Guid senderId, SendMessageDto sendMessageDto)
    {
        var db = _mongoClient.GetDatabase("chatplatform");
        var chatsCollection = db.GetCollection<Chat>("Chats");
        var usersCollection = db.GetCollection<User>("Users");

        // 1. Check if user is in the chat
        var chatFilter = Builders<Chat>.Filter.And(
            Builders<Chat>.Filter.Eq(c => c.Id, sendMessageDto.ChatId),
            Builders<Chat>.Filter.ElemMatch(c => c.Participants,
                Builders<ChatParticipant>.Filter.Eq(p => p.UserId, senderId))
        );

        var chat = await chatsCollection.Find(chatFilter).FirstOrDefaultAsync();
        if (chat == null) throw new UnauthorizedAccessException("User is not in this chat");

        // 2. Get the other user
        var otherUserId = chat.Participants.FirstOrDefault(p => p.UserId != senderId)?.UserId;
        if (otherUserId == null) throw new InvalidOperationException("Cannot determine other user in chat");

        // 3. Verify Permitted Chat (Strict Workflow)
        var canChat = await _chatRequestService.CanUsersChatAsync(senderId, otherUserId.Value);
        if (!canChat) throw new UnauthorizedAccessException("Chat is not enabled yet. Waiting for approval.");

        var message = new Message
        {
            Id = Guid.NewGuid(),
            ChatId = sendMessageDto.ChatId,
            SenderId = senderId,
            Content = sendMessageDto.Content,
            SentAt = DateTime.UtcNow,
            IsRead = false
        };

        // Add message to chat
        var update = Builders<Chat>.Update.Push(c => c.Messages, message);
        await chatsCollection.UpdateOneAsync(Builders<Chat>.Filter.Eq(c => c.Id, sendMessageDto.ChatId), update);

        var sender = await usersCollection.Find(u => u.Id == senderId).FirstOrDefaultAsync();

        return new MessageDto
        {
            Id = message.Id,
            ChatId = message.ChatId,
            SenderId = message.SenderId,
            SenderUsername = sender?.Username ?? string.Empty,
            Content = message.Content,
            SentAt = message.SentAt,
            IsRead = message.IsRead,
            ReadAt = message.ReadAt
        };
    }

    public async Task<List<MessageDto>> GetChatMessagesAsync(Guid chatId, Guid userId, int page = 1, int pageSize = 50)
    {
        var db = _mongoClient.GetDatabase("chatplatform");
        var chatsCollection = db.GetCollection<Chat>("Chats");
        var usersCollection = db.GetCollection<User>("Users");

        // Verification: User in chat
        var chatFilter = Builders<Chat>.Filter.And(
            Builders<Chat>.Filter.Eq(c => c.Id, chatId),
            Builders<Chat>.Filter.ElemMatch(c => c.Participants,
                Builders<ChatParticipant>.Filter.Eq(p => p.UserId, userId))
        );

        var chat = await chatsCollection.Find(chatFilter).FirstOrDefaultAsync();
        if (chat == null) throw new UnauthorizedAccessException("User is not in this chat");

        // Pagination
        var messages = chat.Messages
            .OrderByDescending(m => m.SentAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .OrderBy(m => m.SentAt)
            .ToList();

        var result = new List<MessageDto>();
        foreach (var msg in messages)
        {
            var sender = await usersCollection.Find(u => u.Id == msg.SenderId).FirstOrDefaultAsync();
            result.Add(new MessageDto
            {
                Id = msg.Id,
                ChatId = msg.ChatId,
                SenderId = msg.SenderId,
                SenderUsername = sender?.Username ?? string.Empty,
                Content = msg.Content,
                SentAt = msg.SentAt,
                IsRead = msg.IsRead,
                ReadAt = msg.ReadAt
            });
        }

        return result;
    }

    public async Task MarkAsReadAsync(Guid chatId, Guid userId)
    {
        var db = _mongoClient.GetDatabase("chatplatform");
        var chatsCollection = db.GetCollection<Chat>("Chats");

        var chat = await chatsCollection.Find(Builders<Chat>.Filter.Eq(c => c.Id, chatId)).FirstOrDefaultAsync();
        if (chat != null)
        {
            foreach (var msg in chat.Messages)
            {
                if (msg.ChatId == chatId && msg.SenderId != userId && !msg.IsRead)
                {
                    msg.IsRead = true;
                    msg.ReadAt = DateTime.UtcNow;
                }
            }
            
            var update = Builders<Chat>.Update.Set(c => c.Messages, chat.Messages);
            await chatsCollection.UpdateOneAsync(Builders<Chat>.Filter.Eq(c => c.Id, chatId), update);
        }
    }
}
