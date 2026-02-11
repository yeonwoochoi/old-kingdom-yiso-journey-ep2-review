using ServerShared.DTOs.Rank;
using Yiso.Web.Exceptions;
using Yiso.Web.Repositories.Interfaces;
using Yiso.Web.Services.Interfaces;

namespace Yiso.Web.Services;

public class RankService : IRankService {
    private readonly IRankRepository _rankRepository;

    public RankService(IRankRepository rankRepository) {
        _rankRepository = rankRepository;
    }

    public async Task RegisterScoreAsync(string userId, string username, RankRegisterRequest request) {
        if (request.Score < 0) {
            throw new BadRequestException("점수는 0 이상이어야 합니다.");
        }

        await _rankRepository.RegisterScoreAsync(userId, username, request.Score);
    }

    public async Task<RankListResponse> GetTopRanksAsync(int count) {
        if (count <= 0) count = 10; // default
        return await _rankRepository.GetTopRanksAsync(count);
    }

    public async Task<RankResponse?> GetMyRankAsync(string userId) {
        return await _rankRepository.GetRankByUserIdAsync(userId);
    }

    public async Task<bool> DeleteRankAsync(string userId) {
        return await _rankRepository.DeleteRankByUserIdAsync(userId);
    }
}
