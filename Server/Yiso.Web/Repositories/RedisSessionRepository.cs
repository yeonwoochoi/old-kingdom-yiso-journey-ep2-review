using MemoryPack;
using StackExchange.Redis;
using Yiso.Shared.Models;
using Yiso.Web.Models;
using Yiso.Web.Repositories.Interfaces;

namespace Yiso.Web.Repositories;

/// <summary>
/// Redis 기반 세션 저장소
/// Key 구조:
/// - session:{sessionId} -> SessionData
/// - user_sessions:{userId} -> Set{sessionId1, sessionId2, ...}
/// </summary>
public class RedisSessionRepository : ISessionRepository {
    private readonly IDatabase _db;
    private const string SessionKeyPrefix = "session:";
    private const string UserSessionsKeyPrefix = "user_sessions:";

    public RedisSessionRepository(IConnectionMultiplexer redis) {
        _db = redis.GetDatabase();
    }

    public async Task<string> CreateAsync(SessionData data, TimeSpan expiry) {
        // 기존 세션 무효화 (새 로그인 시 기존 세션 탈취 방지)
        await InvalidateUserSessionsAsync(data.UserId);

        var sessionId = Guid.NewGuid().ToString();
        var sessionKey = GetSessionKey(sessionId);
        var userSessionsKey = GetUserSessionsKey(data.UserId);

        var bytes = MemoryPackSerializer.Serialize(data);

        // 세션 데이터 저장 + user_sessions Set에 추가
        await _db.StringSetAsync(sessionKey, bytes, expiry);
        await _db.SetAddAsync(userSessionsKey, sessionId);

        return sessionId;
    }

    public async Task<SessionData?> GetAsync(string sessionId) {
        var key = GetSessionKey(sessionId);
        var bytes = await _db.StringGetAsync(key);
        if (bytes.IsNullOrEmpty) return null;
        return MemoryPackSerializer.Deserialize<SessionData>((byte[]) bytes!);
    }

    public async Task DeleteAsync(string sessionId) {
        // 세션 데이터 조회해서 userId 확인
        var data = await GetAsync(sessionId);
        if (data != null) {
            // user_sessions Set에서 제거
            var userSessionsKey = GetUserSessionsKey(data.UserId);
            await _db.SetRemoveAsync(userSessionsKey, sessionId);
        }

        // 세션 삭제
        var sessionKey = GetSessionKey(sessionId);
        await _db.KeyDeleteAsync(sessionKey);
    }

    public async Task RefreshAsync(string sessionId, TimeSpan expiry) {
        var key = GetSessionKey(sessionId);

        // 세션 데이터 조회 후 LastAccessedAt 갱신
        var bytes = await _db.StringGetAsync(key);
        if (bytes.IsNullOrEmpty) return;

        var data = MemoryPackSerializer.Deserialize<SessionData>((byte[]) bytes!);
        if (data == null) return;

        data.LastAccessedAt = DateTime.UtcNow;
        var updatedBytes = MemoryPackSerializer.Serialize(data);

        await _db.StringSetAsync(key, updatedBytes, expiry);
    }

    public async Task<bool> ExistsAsync(string sessionId) {
        var key = GetSessionKey(sessionId);
        return await _db.KeyExistsAsync(key);
    }

    public async Task InvalidateUserSessionsAsync(string userId) {
        var userSessionsKey = GetUserSessionsKey(userId);

        // 해당 사용자의 모든 sessionId 조회
        var sessionIds = await _db.SetMembersAsync(userSessionsKey);
        if (sessionIds.Length == 0) return;

        // 모든 세션 키 일괄 삭제
        var sessionKeys = sessionIds.Select(id => (RedisKey) GetSessionKey(id!)).ToArray();
        await _db.KeyDeleteAsync(sessionKeys);

        // user_sessions Set 삭제
        await _db.KeyDeleteAsync(userSessionsKey);
    }

    public async Task<List<string>> GetUserSessionIdsAsync(string userId) {
        var userSessionsKey = GetUserSessionsKey(userId);
        var sessionIds = await _db.SetMembersAsync(userSessionsKey);

        var validSessions = new List<string>();
        foreach (var sessionId in sessionIds) {
            var id = sessionId.ToString();
            // 실제로 존재하는 세션만 반환 (TTL 만료된 것 정리)
            if (await ExistsAsync(id)) {
                validSessions.Add(id);
            } else {
                // 만료된 세션은 Set에서 제거
                await _db.SetRemoveAsync(userSessionsKey, sessionId);
            }
        }

        return validSessions;
    }

    private static string GetSessionKey(string sessionId) => $"{SessionKeyPrefix}{sessionId}";
    private static string GetUserSessionsKey(string userId) => $"{UserSessionsKeyPrefix}{userId}";
}
