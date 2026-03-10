using System;

namespace ChatPlatform.Core.DTOs;

public class SendMessageDto
{
    public Guid ChatId { get; set; }
    public string Content { get; set; } = string.Empty;
}
