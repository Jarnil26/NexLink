using System.Text;
using ChatPlatform.Api.Hubs;
using ChatPlatform.Core.Interfaces;
using ChatPlatform.Infrastructure.Redis;
using ChatPlatform.Infrastructure.Data;
using ChatPlatform.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using MongoDB.Driver;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using Microsoft.IdentityModel.Tokens;
using StackExchange.Redis;

// Configure MongoDB GUID serialization
BsonSerializer.RegisterSerializer(new GuidSerializer(GuidRepresentation.Standard));

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// MongoDB
var mongoConnectionString = builder.Configuration.GetConnectionString("MongoDb");
var mongoSettings = MongoClientSettings.FromConnectionString(mongoConnectionString);
mongoSettings.ConnectTimeout = TimeSpan.FromSeconds(10);
mongoSettings.ServerSelectionTimeout = TimeSpan.FromSeconds(10);
var mongoClient = new MongoClient(mongoSettings);
builder.Services.AddSingleton<IMongoClient>(mongoClient);

// Redis & SignalR Presence Configuration
var redisConnectionString = builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379";
IConnectionMultiplexer redis = null!;
bool isRedisAvailable = false;

try
{
    var probeOptions = ConfigurationOptions.Parse(redisConnectionString);
    probeOptions.AbortOnConnectFail = true;
    probeOptions.ConnectTimeout = 3000;
    
    redis = ConnectionMultiplexer.Connect(probeOptions);
    isRedisAvailable = redis.IsConnected;
}
catch
{
    isRedisAvailable = false;
}

var signalrBuilder = builder.Services.AddSignalR();

if (isRedisAvailable)
{
    builder.Services.AddSingleton<IConnectionMultiplexer>(redis);
    signalrBuilder.AddStackExchangeRedis(redisConnectionString, options => {
        options.Configuration.ChannelPrefix = RedisChannel.Literal("ChatPlatform");
    });
    builder.Services.AddSingleton<IPresenceService, RedisPresenceService>();
}
else
{
    builder.Services.AddSingleton<IConnectionMultiplexer>(x => null!);
    builder.Services.AddSingleton<IPresenceService, InMemoryPresenceService>();
}

// Services DI
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IChatService, ChatService>();
builder.Services.AddScoped<IMessageService, MessageService>();
builder.Services.AddScoped<IConnectionRequestService, ConnectionRequestService>();
builder.Services.AddScoped<IChatRequestService, ChatRequestService>();
builder.Services.AddScoped<IPushNotificationService, PushNotificationService>();

// Authentication
var jwtKey = builder.Configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key is missing");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs/chat"))
                {
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            }
        };
    });

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy", policyBuilder =>
    {
        policyBuilder.SetIsOriginAllowed(origin => true) // Allow any origin dynamically
               .AllowAnyMethod()
               .AllowAnyHeader()
               .AllowCredentials();
    });
});

var app = builder.Build();

app.UseStaticFiles();
app.UseRouting(); // Standard practice: UseRouting before UseCors
app.UseCors("CorsPolicy");
app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new { status = "API is running", timestamp = DateTime.UtcNow }))
    .WithName("Health");

app.MapControllers();
app.MapHub<ChatHub>("/hubs/chat");

using (var scope = app.Services.CreateScope())
{
}

app.Run();
