using System;
using System.Text.Json.Serialization;

namespace ChatPlatform.Core.DTOs;

public class AuthResponseDto
{
    [JsonPropertyName("token")]
    public string Token { get; set; } = string.Empty;

    [JsonPropertyName("user")]
    public UserDto User { get; set; } = null!;
}
