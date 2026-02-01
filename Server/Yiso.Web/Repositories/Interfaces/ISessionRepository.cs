using Yiso.Web.Models;

namespace Yiso.Web.Repositories.Interfaces;

/// <summary>
/// Redis 세션 저장 인터페이스
/// </summary>
public interface ISessionRepository {
    Task<string> CreateAsync(SessionData data, TimeSpan expiry);
    Task<SessionData?> GetAsync(string sessionId);
    Task DeleteAsync(string sessionId);
    Task RefreshAsync(string sessionId, TimeSpan expiry);
    Task<bool> ExistsAsync(string sessionId);
}
