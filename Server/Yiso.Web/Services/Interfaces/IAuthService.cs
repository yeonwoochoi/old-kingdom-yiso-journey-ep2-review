using ServerShared.DTOs.Auth;
using ServerShared.Models;

namespace Yiso.Web.Services.Interfaces;

public interface IAuthService {
    Task<AuthResponse> RegisterAsync(RegisterRequest request);
    Task<AuthResponse> LoginAsync(LoginRequest request);
    Task LogoutAsync(string sessionId);

    /// <summary>
    /// 세션 검증 - 유효한 세션인지 확인 후 SessionData 반환
    /// </summary>
    Task<SessionData?> ValidateSessionAsync(string sessionId);

    /// <summary>
    /// 세션 갱신 - 만료 시간 연장
    /// </summary>
    Task RefreshSessionAsync(string sessionId);
}
