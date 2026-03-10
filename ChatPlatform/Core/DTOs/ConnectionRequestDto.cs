using System;

namespace ChatPlatform.Core.DTOs;

public class ConnectionRequestDto
{
    public Guid Id { get; set; }
    public Guid FromUserId { get; set; }
    public string FromUsername { get; set; } = string.Empty;
    public Guid ToUserId { get; set; }
    public string Status { get; set; } = "Pending";
    public DateTime CreatedAt { get; set; }
    public DateTime? RespondedAt { get; set; }
}

public class CreateConnectionRequestDto
{
    public Guid TargetUserId { get; set; }
}

public class UpdateConnectionRequestDto
{
    public string Status { get; set; } = "Pending"; // "Accepted" or "Rejected"
}
