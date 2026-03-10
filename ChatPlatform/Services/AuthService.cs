using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using ChatPlatform.Core.DTOs;
using ChatPlatform.Core.Entities;
using ChatPlatform.Core.Interfaces;
using MongoDB.Driver;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace ChatPlatform.Services;

public class AuthService : IAuthService
{
    private readonly IMongoClient _mongoClient;
    private readonly IConfiguration _configuration;

    public AuthService(IMongoClient mongoClient, IConfiguration configuration)
    {
        _mongoClient = mongoClient;
        _configuration = configuration;
    }

    public async Task<AuthResponseDto> LoginAsync(LoginDto loginDto)
    {
        var db = _mongoClient.GetDatabase("chatplatform");
        var usersCollection = db.GetCollection<User>("Users");
        var user = await usersCollection.Find(u => u.Username == loginDto.Username).FirstOrDefaultAsync();
        if (user == null || !BCrypt.Net.BCrypt.Verify(loginDto.Password, user.PasswordHash))
        {
            throw new Exception("Invalid username or password");
        }

        var token = GenerateJwtToken(user);
        
        return new AuthResponseDto
        {
            Token = token,
            User = new UserDto
            {
                Id = user.Id,
                Username = user.Username,
                Avatar = user.Avatar,
                IsOnline = user.IsOnline
            }
        };
    }

    public async Task<AuthResponseDto> RegisterAsync(RegisterDto registerDto)
    {
        var db = _mongoClient.GetDatabase("chatplatform");
        var usersCollection = db.GetCollection<User>("Users");
        
        var existingUser = await usersCollection.Find(u => u.Username == registerDto.Username).FirstOrDefaultAsync();
        if (existingUser != null)
        {
            throw new Exception("Username already exists");
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = registerDto.Username,
            Email = registerDto.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(registerDto.Password),
            IsOnline = false
        };

        await usersCollection.InsertOneAsync(user);

        var token = GenerateJwtToken(user);

        return new AuthResponseDto
        {
            Token = token,
            User = new UserDto
            {
                Id = user.Id,
                Username = user.Username,
                Avatar = user.Avatar,
                IsOnline = user.IsOnline
            }
        };
    }

    private string GenerateJwtToken(User user)
    {
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.UniqueName, user.Username),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddDays(7),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
