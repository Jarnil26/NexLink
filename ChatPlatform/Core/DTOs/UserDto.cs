using System;
using System.Text.Json.Serialization;

namespace ChatPlatform.Core.DTOs;

public class UserDto
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("username")]
    public string Username { get; set; } = string.Empty;

    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;

    [JsonPropertyName("avatar")]
    public string Avatar { get; set; } = string.Empty;

    [JsonPropertyName("theme")]
    public string Theme { get; set; } = "system";

    [JsonPropertyName("isOnline")]
    public bool IsOnline { get; set; }

    [JsonPropertyName("connectionStatus")]
    public string? ConnectionStatus { get; set; } 

    [JsonPropertyName("chatRequestStatus")]
    public string? ChatRequestStatus { get; set; }
}
