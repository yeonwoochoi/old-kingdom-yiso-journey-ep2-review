using Yiso.Web.DTOs;
using Yiso.Web.Models;
using Yiso.Web.Repositories.Interfaces;
using Yiso.Web.Services.Interfaces;

namespace Yiso.Web.Services;

public class AuthService : IAuthService {
    private readonly IUserRepository _userRepository;
    private readonly IPasswordService _passwordService;
    private readonly IJwtService _jwtService;

    public AuthService(
        IUserRepository userRepository,
        IPasswordService passwordService,
        IJwtService jwtService) {
        _userRepository = userRepository;
        _passwordService = passwordService;
        _jwtService = jwtService;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request) {
        if (await _userRepository.ExistsAsync(request.Username)) {
            throw new InvalidOperationException("이미 존재하는 사용자 이름입니다.");
        }

        var user = new User {
            Username = request.Username,
            PasswordHash = _passwordService.HashPassword(request.Password)
        };

        await _userRepository.CreateAsync(user);

        return new AuthResponse {
            Token = _jwtService.GenerateToken(user),
            Username = user.Username,
            ExpiresAt = _jwtService.GetExpirationTime()
        };
    }
    
    public async Task<AuthResponse> LoginAsync(LoginRequest request) {
        var user = await _userRepository.GetByUsernameAsync(request.Username);

        if (user == null || !_passwordService.VerifyPassword(request.Password, user.PasswordHash)) {
            throw new UnauthorizedAccessException("잘못된 사용자 이름 또는 비밀번호입니다.");
        }

        return new AuthResponse {
            Token = _jwtService.GenerateToken(user),
            Username = user.Username,
            ExpiresAt = _jwtService.GetExpirationTime()
        };
    }

    public async Task<User?> GetUserByIdAsync(string userId) {
        return await _userRepository.GetByIdAsync(userId);
    }
}
