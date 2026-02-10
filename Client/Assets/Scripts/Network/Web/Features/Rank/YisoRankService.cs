using System.Threading.Tasks;
using Network.Web.Core;
using ServerShared.DTOs.Rank;

namespace Network.Web.Features.Rank {
    /// <summary>
    /// 랭킹 API 호출 서비스
    /// </summary>
    public class YisoRankService : IYisoRankService {
        private readonly YisoHttpClient httpClient;

        public YisoRankService(YisoHttpClient httpClient) {
            this.httpClient = httpClient;
        }

        /// <summary>
        /// 점수 등록
        /// </summary>
        public async Task<YisoHttpResponse> RegisterScoreAsync(int score) {
            var request = new RankRegisterRequest { Score = score };
            return await httpClient.PostAsync(YisoApiEndpoints.Rank.Score, request);
        }

        /// <summary>
        /// Top N 랭킹 조회
        /// </summary>
        public async Task<YisoHttpResponse<RankListResponse>> GetTopRanksAsync(int count = 10) {
            return await httpClient.GetAsync<RankListResponse>($"{YisoApiEndpoints.Rank.Top}?count={count}");
        }

        /// <summary>
        /// 내 랭킹 조회
        /// </summary>
        public async Task<YisoHttpResponse<RankResponse>> GetMyRankAsync() {
            return await httpClient.GetAsync<RankResponse>(YisoApiEndpoints.Rank.Me);
        }

        /// <summary>
        /// 내 랭킹 삭제
        /// </summary>
        public async Task<YisoHttpResponse> DeleteMyRankAsync() {
            return await httpClient.DeleteAsync(YisoApiEndpoints.Rank.Me);
        }
    }
}