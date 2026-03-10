using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ChatPlatform.Core.DTOs;
using ChatPlatform.Core.Entities;
using ChatPlatform.Core.Interfaces;
using MongoDB.Driver;

namespace ChatPlatform.Services;

public class ChatService : IChatService
{
    private readonly IMongoClient _mongoClient;

    public ChatService(IMongoClient mongoClient)
    {
        _mongoClient = mongoClient;
    }

    public async Task<ChatDto> CreateOrGetOneToOneChatAsync(Guid currentUserId, Guid otherUserId)
    {
        var db = _mongoClient.GetDatabase("chatplatform");
        var chatsCollection = db.GetCollection<Chat>("Chats");
        var usersCollection = db.GetCollection<User>("Users");

        // Find existing one-to-one chat
        var filter = Builders<Chat>.Filter.And(
            Builders<Chat>.Filter.Eq(c => c.IsGroup, false),
            Builders<Chat>.Filter.ElemMatch(c => c.Participants, 
                Builders<ChatParticipant>.Filter.Eq(p => p.UserId, currentUserId)),
            Builders<Chat>.Filter.ElemMatch(c => c.Participants, 
                Builders<ChatParticipant>.Filter.Eq(p => p.UserId, otherUserId))
        );

        var existingChat = await chatsCollection.Find(filter).FirstOrDefaultAsync();
        if (existingChat != null)
        {
            return await MapToDtoAsync(existingChat, usersCollection, currentUserId);
        }

        // Create new one-to-one chat
        var newChat = new Chat 
        { 
            Id = Guid.NewGuid(),
            IsGroup = false,
            CreatedAt = DateTime.UtcNow,
            Participants = new List<ChatParticipant>
            {
                new ChatParticipant { UserId = currentUserId },
                new ChatParticipant { UserId = otherUserId }
            },
            Messages = new List<Message>()
        };

        await chatsCollection.InsertOneAsync(newChat);
        return await MapToDtoAsync(newChat, usersCollection, currentUserId);
    }

    public async Task<List<ChatDto>> GetUserChatsAsync(Guid userId)
    {
        var db = _mongoClient.GetDatabase("chatplatform");
        var chatsCollection = db.GetCollection<Chat>("Chats");
        var usersCollection = db.GetCollection<User>("Users");

        var filter = Builders<Chat>.Filter.ElemMatch(c => c.Participants,
            Builders<ChatParticipant>.Filter.Eq(p => p.UserId, userId));

        var chats = await chatsCollection.Find(filter).ToListAsync();
        
        var result = new List<ChatDto>();
        foreach (var chat in chats)
        {
            result.Add(await MapToDtoAsync(chat, usersCollection, userId));
        }
        
        return result;
    }

    public async Task<bool> IsUserInChatAsync(Guid userId, Guid chatId)
    {
        var db = _mongoClient.GetDatabase("chatplatform");
        var chatsCollection = db.GetCollection<Chat>("Chats");

        var filter = Builders<Chat>.Filter.And(
            Builders<Chat>.Filter.Eq(c => c.Id, chatId),
            Builders<Chat>.Filter.ElemMatch(c => c.Participants,
                Builders<ChatParticipant>.Filter.Eq(p => p.UserId, userId))
        );

        var chat = await chatsCollection.Find(filter).FirstOrDefaultAsync();
        return chat != null;
    }

    private async Task<ChatDto> MapToDtoAsync(Chat chat, IMongoCollection<User> usersCollection, Guid currentUserId)
    {
        var participantDtos = new List<UserDto>();
        foreach (var participant in chat.Participants)
        {
            var user = await usersCollection.Find(u => u.Id == participant.UserId).FirstOrDefaultAsync();
            if (user != null)
            {
                participantDtos.Add(new UserDto
                {
                    Id = user.Id,
                    Username = user.Username,
                    Avatar = user.Avatar,
                    IsOnline = user.IsOnline
                });
            }
        }

        MessageDto? lastMessageDto = null;
        if (chat.Messages.Any())
        {
            var lastMsg = chat.Messages.OrderByDescending(m => m.SentAt).First();
            var sender = await usersCollection.Find(u => u.Id == lastMsg.SenderId).FirstOrDefaultAsync();
            lastMessageDto = new MessageDto
            {
                Id = lastMsg.Id,
                ChatId = lastMsg.ChatId,
                Content = lastMsg.Content,
                SentAt = lastMsg.SentAt,
                SenderId = lastMsg.SenderId,
                SenderUsername = sender?.Username ?? string.Empty,
                IsRead = lastMsg.IsRead
            };
        }

        int unreadCount = chat.Messages.Count(m => !m.IsRead && m.SenderId != currentUserId);

        return new ChatDto
        {
            Id = chat.Id,
            IsGroup = chat.IsGroup,
            Name = chat.Name,
            Participants = participantDtos,
            LastMessage = lastMessageDto,
            UnreadMessageCount = unreadCount
        };
    }
}
