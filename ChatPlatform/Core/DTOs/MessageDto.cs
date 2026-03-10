using System;
using System.Text.Json.Serialization;

namespace ChatPlatform.Core.DTOs;

public class MessageDto
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("chatId")]
    public Guid ChatId { get; set; }

    [JsonPropertyName("senderId")]
    public Guid SenderId { get; set; }

    [JsonPropertyName("senderUsername")]
    public string SenderUsername { get; set; } = string.Empty;

    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;

    [JsonPropertyName("sentAt")]
    public DateTime SentAt { get; set; }

    [JsonPropertyName("isRead")]
    public bool IsRead { get; set; }

    [JsonPropertyName("readAt")]
    public DateTime? ReadAt { get; set; }
}
