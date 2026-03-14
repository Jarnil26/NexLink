using System.Threading.Tasks;
using ChatPlatform.Core.Entities;
using ChatPlatform.Services;
using ChatPlatform.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using Microsoft.Extensions.Configuration;
using System.Linq;

namespace ChatPlatform.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PushController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly IMongoClient _mongoClient;

    public PushController(IConfiguration configuration, IMongoClient mongoClient)
    {
        _configuration = configuration;
        _mongoClient = mongoClient;
    }

    [HttpGet("vapidPublicKey")]
    [AllowAnonymous]
    public IActionResult GetVapidPublicKey()
    {
        var publicKey = _configuration["VAPID:PublicKey"];
        return Ok(new { publicKey });
    }

    [HttpPost("subscribe")]
    public async Task<IActionResult> Subscribe([FromBody] PushSubscriptionModel subscription)
    {
        var userIdString = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdString) || !System.Guid.TryParse(userIdString, out var userId))
        {
            return Unauthorized();
        }

        var db = _mongoClient.GetDatabase("chatplatform");
        var usersCollection = db.GetCollection<User>("Users");
        var user = await usersCollection.Find(u => u.Id == userId).FirstOrDefaultAsync();
        
        if (user == null) return NotFound();

        // Check if already subscribed
        if (!user.PushSubscriptions.Any(s => s.Endpoint == subscription.Endpoint))
        {
            user.PushSubscriptions.Add(subscription);
            await usersCollection.ReplaceOneAsync(u => u.Id == user.Id, user);
        }

        return Ok(new { message = "Subscribed successfully" });
    }

    [HttpPost("unsubscribe")]
    public async Task<IActionResult> Unsubscribe([FromBody] PushSubscriptionModel subscription)
    {
        var userIdString = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdString) || !System.Guid.TryParse(userIdString, out var userId))
        {
            return Unauthorized();
        }

        var db = _mongoClient.GetDatabase("chatplatform");
        var usersCollection = db.GetCollection<User>("Users");
        var user = await usersCollection.Find(u => u.Id == userId).FirstOrDefaultAsync();
        
        if (user == null) return NotFound();

        var subToRemove = user.PushSubscriptions.FirstOrDefault(s => s.Endpoint == subscription.Endpoint);
        if (subToRemove != null)
        {
            user.PushSubscriptions.Remove(subToRemove);
            await usersCollection.ReplaceOneAsync(u => u.Id == user.Id, user);
        }

        return Ok(new { message = "Unsubscribed successfully" });
    }
}
