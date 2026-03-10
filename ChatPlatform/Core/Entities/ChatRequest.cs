using System;

namespace ChatPlatform.Core.Entities;

public class ChatRequest
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid FromUserId { get; set; }  // User wanting to chat
    public Guid ToUserId { get; set; }    // User being asked to chat
    public Guid ConnectionId { get; set; } // Link to the accepted connection request
    public Guid? ChatId { get; set; }      // The chat that will be unlocked (assigned on acceptance)
    public string Status { get; set; } = "Pending"; // Pending, Accepted, Rejected, Blocked, Reported
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? RespondedAt { get; set; }
}
