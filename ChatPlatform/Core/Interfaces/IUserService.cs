using System;
using System.Threading.Tasks;
using ChatPlatform.Core.DTOs;

namespace ChatPlatform.Core.Interfaces;

public interface IUserService
{
    Task<UserDto?> GetUserByIdAsync(Guid userId);
    Task<UserDto?> GetUserByUsernameAsync(string username);
    Task<List<UserDto>> SearchUsersAsync(string query, Guid currentUserId);
    Task UpdateUserStatusAsync(Guid userId, bool isOnline);
}
