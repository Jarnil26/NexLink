using System;
using MongoDB.Bson.Serialization.Attributes;

namespace ChatPlatform.Core.Entities;

[BsonIgnoreExtraElements]
public class PushSubscriptionModel
{
    [BsonElement("endpoint")]
    public string Endpoint { get; set; } = string.Empty;

    [BsonElement("p256dh")]
    public string P256dh { get; set; } = string.Empty;

    [BsonElement("auth")]
    public string Auth { get; set; } = string.Empty;
}
