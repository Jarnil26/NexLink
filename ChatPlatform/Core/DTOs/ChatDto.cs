using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ChatPlatform.Core.DTOs;

public class ChatDto
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("isGroup")]
    public bool IsGroup { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("participants")]
    public List<UserDto> Participants { get; set; } = new List<UserDto>();

    [JsonPropertyName("lastMessage")]
    public MessageDto? LastMessage { get; set; }

    [JsonPropertyName("unreadMessageCount")]
    public int UnreadMessageCount { get; set; }
}
