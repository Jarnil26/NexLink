using System.Threading.Tasks;
using ChatPlatform.Core.DTOs;

namespace ChatPlatform.Core.Interfaces;

public interface IAuthService
{
    Task<AuthResponseDto> LoginAsync(LoginDto loginDto);
    Task<AuthResponseDto> RegisterAsync(RegisterDto registerDto);
}
