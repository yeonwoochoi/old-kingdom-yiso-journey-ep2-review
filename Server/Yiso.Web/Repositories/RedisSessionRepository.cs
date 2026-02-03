using MemoryPack;
using StackExchange.Redis;
using Yiso.Shared.Models;
using Yiso.Web.Models;
using Yiso.Web.Repositories.Interfaces;

namespace Yiso.Web.Repositories;

/// <summary>
/// Redis 기반 세션 저장소
/// Key 형식: session:{sessionId}
/// </summary>
public class RedisSessionRepository : ISessionRepository {
    private readonly IDatabase _db;
    private const string KeyPrefix = "session:"; // Redis는 키가 하나 저장소에 저장되기 때문에 안겹치게 하려면 prefix 붙이는 게 좋음

    public RedisSessionRepository(IConnectionMultiplexer redis) {
        _db = redis.GetDatabase();
    }

    public async Task<string> CreateAsync(SessionData data, TimeSpan expiry) {
        var sessionId = Guid.NewGuid().ToString();
        var key = GetKey(sessionId);
        var bytes = MemoryPackSerializer.Serialize(data);
        await _db.StringSetAsync(key, bytes, expiry);
        return sessionId;
    }

    public async Task<SessionData?> GetAsync(string sessionId) {
        var key = GetKey(sessionId);
        var bytes = await _db.StringGetAsync(key);
        if (bytes.IsNullOrEmpty) return null;
        return MemoryPackSerializer.Deserialize<SessionData>((byte[]) bytes!);
    }

    public async Task DeleteAsync(string sessionId) {
        var key = GetKey(sessionId);
        await _db.KeyDeleteAsync(key);
    }

    public async Task RefreshAsync(string sessionId, TimeSpan expiry) {
        var key = GetKey(sessionId);

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
        var key = GetKey(sessionId);
        return await _db.KeyExistsAsync(key);
    }

    private static string GetKey(string sessionId) => $"{KeyPrefix}{sessionId}";
}
