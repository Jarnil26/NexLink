using System;
using System.Collections.Generic;

namespace ChatPlatform.Core.Entities;

public class Chat
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public bool IsGroup { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<ChatParticipant> Participants { get; set; } = new List<ChatParticipant>();
    public ICollection<Message> Messages { get; set; } = new List<Message>();
}
