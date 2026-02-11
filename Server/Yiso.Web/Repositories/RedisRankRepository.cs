using ServerShared.DTOs.Rank;
using StackExchange.Redis;
using Yiso.Web.Repositories.Interfaces;

namespace Yiso.Web.Repositories;

/// <summary>
/// Redis Sorted Set 기반 랭킹 저장소
/// Key 구조:
/// - rank:leaderboard -> Sorted Set (member=userId, score=effectiveScore)
/// - rank:usernames -> Hash (userId -> username)
///
/// 동점 처리:
/// effectiveScore = score + (MaxTimestamp - 등록시간) / MaxTimestamp
/// 같은 점수일 때 먼저 등록한 유저가 소수부가 더 크므로 높은 등수를 받음
/// 실제 점수는 정수부만 사용 (소수부는 정렬용 타임스탬프)
/// </summary>
public class RedisRankRepository : IRankRepository {
    private readonly IDatabase _db;
    private const string LeaderboardKey = "rank:leaderboard";
    private const string UsernamesKey = "rank:usernames";

    // ~2286년까지 커버, 소수부가 항상 0~1 사이가 되도록 보장
    private const long MaxTimestamp = 10_000_000_000L;

    public RedisRankRepository(IConnectionMultiplexer redis) {
        _db = redis.GetDatabase();
    }

    public async Task RegisterScoreAsync(string userId, string username, int score) {
        // 동점 시 먼저 등록한 유저가 높은 등수를 받도록 타임스탬프를 소수부에 인코딩
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var effectiveScore = score + (MaxTimestamp - timestamp) / (double)MaxTimestamp;

        await _db.SortedSetAddAsync(LeaderboardKey, userId, effectiveScore);
        // userId -> username 매핑 저장
        await _db.HashSetAsync(UsernamesKey, userId, username);
    }

    public async Task<RankListResponse> GetTopRanksAsync(int count) {
        // 점수 높은 순으로 Top N 조회
        var entries = await _db.SortedSetRangeByRankWithScoresAsync(LeaderboardKey, 0, count - 1, Order.Descending);

        var userIds = entries.Select(e => (RedisValue)e.Element.ToString()).ToArray();
        var usernames = await _db.HashGetAsync(UsernamesKey, userIds);

        var ranks = new List<RankResponse>();
        for (var i = 0; i < entries.Length; i++) {
            ranks.Add(new RankResponse {
                Username = usernames[i].ToString() ?? string.Empty,
                Score = (int)Math.Floor(entries[i].Score),
                Rank = i + 1
            });
        }

        return new RankListResponse { Ranks = ranks };
    }

    public async Task<RankResponse?> GetRankByUserIdAsync(string userId) {
        var score = await _db.SortedSetScoreAsync(LeaderboardKey, userId);
        if (score == null) return null;

        // 내림차순 순위 조회
        var rank = await _db.SortedSetRankAsync(LeaderboardKey, userId, Order.Descending);
        if (rank == null) return null;

        var username = await _db.HashGetAsync(UsernamesKey, userId);

        return new RankResponse {
            Username = username.ToString() ?? string.Empty,
            Score = (int)Math.Floor(score.Value),
            Rank = (int)rank.Value + 1
        };
    }

    public async Task<bool> DeleteRankByUserIdAsync(string userId) {
        var removed = await _db.SortedSetRemoveAsync(LeaderboardKey, userId);
        if (removed) {
            await _db.HashDeleteAsync(UsernamesKey, userId);
        }
        return removed;
    }
}
