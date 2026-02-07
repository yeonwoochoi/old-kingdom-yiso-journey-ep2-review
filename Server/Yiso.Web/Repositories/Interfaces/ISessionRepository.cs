using ServerShared.Models;

namespace Yiso.Web.Repositories.Interfaces;

/// <summary>
/// Redis 세션 저장 인터페이스
/// Key 구조:
/// - session:{sessionId} -> SessionData
/// - user_sessions:{userId} -> Set{sessionId1, sessionId2, ...}
/// </summary>
public interface ISessionRepository {
    Task<string> CreateAsync(SessionData data, TimeSpan expiry);
    Task<SessionData?> GetAsync(string sessionId);
    Task DeleteAsync(string sessionId);
    Task RefreshAsync(string sessionId, TimeSpan expiry);
    Task<bool> ExistsAsync(string sessionId);

    /// <summary>
    /// 특정 사용자의 모든 세션을 무효화
    /// </summary>
    Task InvalidateUserSessionsAsync(string userId);

    /// <summary>
    /// 특정 사용자의 활성 세션 ID 목록 조회
    /// </summary>
    Task<List<string>> GetUserSessionIdsAsync(string userId);
}
