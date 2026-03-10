using System;
using System.Collections.Generic;
using MongoDB.Bson.Serialization.Attributes;

namespace ChatPlatform.Core.Entities;

[BsonIgnoreExtraElements]
public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [BsonElement("username")]
    public string Username { get; set; } = string.Empty;

    [BsonElement("email")]
    public string Email { get; set; } = string.Empty;

    [BsonElement("passwordHash")]
    public string PasswordHash { get; set; } = string.Empty;

    [BsonElement("avatar")]
    public string Avatar { get; set; } = string.Empty;

    [BsonElement("theme")]
    public string Theme { get; set; } = "system";

    [BsonElement("isOnline")]
    public bool IsOnline { get; set; }

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<ChatParticipant> ChatParticipants { get; set; } = new List<ChatParticipant>();
}
