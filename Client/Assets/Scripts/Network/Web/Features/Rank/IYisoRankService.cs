using System.Threading.Tasks;
using Network.Web.Core;
using ServerShared.DTOs.Rank;

namespace Network.Web.Features.Rank {
    /// <summary>
    /// 랭킹 API 호출 서비스
    /// </summary>
    public interface IYisoRankService {
        /// <summary>
        /// 점수 등록 (세션 ID 필요)
        /// </summary>
        Task<YisoHttpResponse> RegisterScoreAsync(int score);

        /// <summary>
        /// Top N 랭킹 조회
        /// </summary>
        Task<YisoHttpResponse<RankListResponse>> GetTopRanksAsync(int count = 10);

        /// <summary>
        /// 내 랭킹 조회 (세션 ID 필요)
        /// </summary>
        Task<YisoHttpResponse<RankResponse>> GetMyRankAsync();

        /// <summary>
        /// 내 랭킹 삭제 (세션 ID 필요)
        /// </summary>
        Task<YisoHttpResponse> DeleteMyRankAsync();
    }
}