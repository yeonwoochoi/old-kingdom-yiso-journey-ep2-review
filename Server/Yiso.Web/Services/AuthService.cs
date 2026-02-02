using Yiso.Web.DTOs;
using Yiso.Web.Exceptions;
using Yiso.Web.Models;
using Yiso.Web.Repositories.Interfaces;
using Yiso.Web.Services.Interfaces;

namespace Yiso.Web.Services;

public class AuthService : IAuthService {
    private readonly IUserRepository _userRepository;
    private readonly ISessionRepository _sessionRepository;
    private readonly IPasswordService _passwordService;
    private TimeSpan _sessionExpiry;
    
    public AuthService(IUserRepository userRepository, ISessionRepository sessionRepository, IPasswordService passwordService, IConfiguration configuration) {
        _userRepository = userRepository;
        _sessionRepository = sessionRepository;
        _passwordService = passwordService;
        
        var timeoutMinutes = configuration.GetValue<int>("Session:TimeoutMinutes", 30); // appsettings에서 읽어옴 (세션 ttl)
        _sessionExpiry = TimeSpan.FromMinutes(timeoutMinutes);
    }


    public async Task<AuthResponse> RegisterAsync(RegisterRequest request) {
        if (await _userRepository.ExistsAsync(request.Username)) {
            throw new ConflictException("이미 존재하는 사용자 이름입니다.");
        }

        // 유저 생성
        var user = new User {
            Username = request.Username,
            PasswordHash = _passwordService.HashPassword(request.Password)
        };

        await _userRepository.CreateAsync(user);

        // 세션 생성
        var sessionData = new SessionData {
            UserId = user.Id,
            Username = user.Username
        };
        var sessionId = await _sessionRepository.CreateAsync(sessionData, _sessionExpiry);

        return new AuthResponse {
            SessionId = sessionId,
            Username = user.Username,
            ExpiresAt = DateTime.UtcNow.Add(_sessionExpiry)
        };
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request) {
        var user = await _userRepository.GetByUsernameAsync(request.Username);

        if (user == null || !_passwordService.VerifyPassword(request.Password, user.PasswordHash)) {
            throw new UnauthorizedException("잘못된 사용자 이름 또는 비밀번호입니다.");
        }
        
        var sessionData = new SessionData {
            UserId = user.Id,
            Username = user.Username
        };
        var sessionId = await _sessionRepository.CreateAsync(sessionData, _sessionExpiry);

        return new AuthResponse {
            SessionId = sessionId,
            Username = user.Username,
            ExpiresAt = DateTime.UtcNow.Add(_sessionExpiry)
        };
    }

    public async Task LogoutAsync(string sessionId) {
        await _sessionRepository.DeleteAsync(sessionId);
    }

    public async Task<SessionData?> ValidateSessionAsync(string sessionId) {
        return await _sessionRepository.GetAsync(sessionId);
    }

    public async Task RefreshSessionAsync(string sessionId) {
        await _sessionRepository.RefreshAsync(sessionId, _sessionExpiry);
    }
}
