using System.Threading.Tasks;
using Network.Web.Core;
using Network.Web.Features.Auth.DTOs;

namespace Network.Web.Features.Auth {
    public class YisoAuthService : IYisoAuthService {
        private readonly YisoHttpClient httpClient;

        public YisoAuthService(YisoHttpClient httpClient) {
            this.httpClient = httpClient;
        }

        /// <summary>
        /// 회원가입
        /// </summary>
        public async Task<YisoHttpResponse<YisoAuthResponse>> RegisterAsync(string username, string password) {
            var request = new YisoRegisterRequest(username, password);
            return await httpClient.PostAsync<YisoAuthResponse>(YisoApiEndpoints.Auth.Register, request);
        }

        /// <summary>
        /// 로그인
        /// </summary>
        public async Task<YisoHttpResponse<YisoAuthResponse>> LoginAsync(string username, string password) {
            var request = new YisoLoginRequest(username, password);
            return await httpClient.PostAsync<YisoAuthResponse>(YisoApiEndpoints.Auth.Login, request);
        }

        /// <summary>
        /// 현재 사용자 정보 조회
        /// </summary>
        public async Task<YisoHttpResponse<YisoUserInfo>> GetCurrentUserAsync() {
            return await httpClient.GetAsync<YisoUserInfo>(YisoApiEndpoints.Auth.Me);
        }
    }
}
