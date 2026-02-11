using ServerShared.DTOs.Rank;

namespace Yiso.Web.Repositories.Interfaces;

public interface IRankRepository {
    Task RegisterScoreAsync(string userId, string username, int score);
    Task<RankListResponse> GetTopRanksAsync(int count);
    Task<RankResponse?> GetRankByUserIdAsync(string userId);
    Task<bool> DeleteRankByUserIdAsync(string userId);
}