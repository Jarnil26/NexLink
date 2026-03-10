using System;

namespace ChatPlatform.Core.Entities;

public class Report
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ReporterUserId { get; set; }
    public Guid ReportedUserId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string Category { get; set; } = "General"; // Connection, Chat, Message, etc.
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
