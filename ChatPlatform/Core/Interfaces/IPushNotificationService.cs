using System.Threading.Tasks;
using ChatPlatform.Core.Entities;
using System.Collections.Generic;

namespace ChatPlatform.Core.Interfaces;

public interface IPushNotificationService
{
    Task SendPushNotificationAsync(IEnumerable<PushSubscriptionModel> subscriptions, string title, string body, string url);
}
