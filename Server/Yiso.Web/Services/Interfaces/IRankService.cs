using ServerShared.DTOs.Rank;

namespace Yiso.Web.Services.Interfaces;

public interface IRankService {
    Task RegisterScoreAsync(string userId, string username, RankRegisterRequest request);
    Task<RankListResponse> GetTopRanksAsync(int count);
    Task<RankResponse?> GetMyRankAsync(string userId);
    Task DeleteRankAsync(string userId);
}
