using ServerShared.DTOs.Rank;
using StackExchange.Redis;
using Yiso.Web.Repositories.Interfaces;

namespace Yiso.Web.Repositories;

/// <summary>
/// Redis Sorted Set 기반 랭킹 저장소
/// Key 구조:
/// - rank:leaderboard -> Sorted Set (member=userId, score=score)
/// - rank:usernames -> Hash (userId -> username)
/// </summary>
public class RedisRankRepository : IRankRepository {
    private readonly IDatabase _db;
    private const string LeaderboardKey = "rank:leaderboard";
    private const string UsernamesKey = "rank:usernames";

    public RedisRankRepository(IConnectionMultiplexer redis) {
        _db = redis.GetDatabase();
    }

    public async Task RegisterScoreAsync(string userId, string username, int score) {
        // Sorted Set에 점수 등록 (기존 점수가 있으면 덮어쓰기)
        await _db.SortedSetAddAsync(LeaderboardKey, userId, score);
        // userId -> username 매핑 저장
        await _db.HashSetAsync(UsernamesKey, userId, username);
    }

    public async Task<RankListResponse> GetTopRanksAsync(int count) {
        // 점수 높은 순으로 Top N 조회
        // entries 안에 { Element = "user1", Score = 3000 } 같은 데이터 배열 있음.
        var entries = await _db.SortedSetRangeByRankWithScoresAsync(LeaderboardKey, 0, count - 1, Order.Descending);

        var userIds = entries.Select(e => (RedisValue)e.Element.ToString()).ToArray();
        var usernames = await _db.HashGetAsync(UsernamesKey, userIds);

        var ranks = new List<RankResponse>();
        for (var i = 0; i < entries.Length; i++) {
            ranks.Add(new RankResponse {
                Username = usernames[i].ToString() ?? string.Empty,
                Score = (int)entries[i].Score,
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
            Score = (int)score.Value,
            Rank = (int)rank.Value + 1
        };
    }

    public async Task DeleteRankByUserIdAsync(string userId) {
        await _db.SortedSetRemoveAsync(LeaderboardKey, userId);
        await _db.HashDeleteAsync(UsernamesKey, userId);
    }
}
