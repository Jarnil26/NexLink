using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ChatPlatform.Core.Entities;
using ChatPlatform.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using WebPush;
using System.Text.Json;

namespace ChatPlatform.Services;

public class PushNotificationService : IPushNotificationService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<PushNotificationService> _logger;
    private readonly WebPushClient _webPushClient;

    public PushNotificationService(IConfiguration configuration, ILogger<PushNotificationService> logger)
    {
        _configuration = configuration;
        _logger = logger;
        _webPushClient = new WebPushClient();
    }

    public async Task SendPushNotificationAsync(IEnumerable<PushSubscriptionModel> subscriptions, string title, string body, string url)
    {
        var subject = _configuration["VAPID:Subject"] ?? "mailto:admin@example.com";
        var publicKey = _configuration["VAPID:PublicKey"];
        var privateKey = _configuration["VAPID:PrivateKey"];

        if (string.IsNullOrEmpty(publicKey) || string.IsNullOrEmpty(privateKey))
        {
            _logger.LogWarning("VAPID keys are missing. Cannot send push notification.");
            return;
        }

        var vapidDetails = new VapidDetails(subject, publicKey, privateKey);
        
        var payload = JsonSerializer.Serialize(new
        {
            notification = new
            {
                title = title,
                body = body,
                icon = "/assets/icons/icon-192x192.png",
                vibrate = new[] { 100, 50, 100 },
                data = new
                {
                    url = url
                }
            }
        });

        foreach (var sub in subscriptions)
        {
            try
            {
                var pushSubscription = new PushSubscription(sub.Endpoint, sub.P256dh, sub.Auth);
                await _webPushClient.SendNotificationAsync(pushSubscription, payload, vapidDetails);
            }
            catch (WebPushException exception)
            {
                var statusCode = exception.StatusCode;
                _logger.LogError(exception, $"Push Error: {statusCode}");
                // If 410 Gone, the subscription is expired and should be removed from the DB,
                // but for simplicity we just log it here.
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send push notification.");
            }
        }
    }
}
