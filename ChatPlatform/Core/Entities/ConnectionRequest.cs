using System;

namespace ChatPlatform.Core.Entities;

public class ConnectionRequest
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid FromUserId { get; set; }  // User sending request
    public Guid ToUserId { get; set; }    // User receiving request
    public string Status { get; set; } = "Pending"; // Pending, Accepted, Rejected
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? RespondedAt { get; set; }
}
