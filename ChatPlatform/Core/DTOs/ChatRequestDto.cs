using System;

namespace ChatPlatform.Core.DTOs;

public class ChatRequestDto
{
    public Guid Id { get; set; }
    public Guid FromUserId { get; set; }
    public string FromUsername { get; set; } = string.Empty;
    public Guid ToUserId { get; set; }
    public Guid ChatId { get; set; }
    public string Status { get; set; } = "Pending";
    public DateTime CreatedAt { get; set; }
    public DateTime? RespondedAt { get; set; }
}

public class CreateChatRequestDto
{
    public Guid TargetUserId { get; set; }
    public Guid ChatId { get; set; }
}

public class UpdateChatRequestDto
{
    public string Status { get; set; } = "Pending"; // "Accepted" or "Rejected"
}
