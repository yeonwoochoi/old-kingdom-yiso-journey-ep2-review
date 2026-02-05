using System.Threading.Tasks;
using Network.Web.Core;
using Network.Web.Features.Auth.DTOs;
using Yiso.Shared.DTOs.Auth;

namespace Network.Web.Features.Auth {
    public class YisoAuthService : IYisoAuthService {
        private readonly YisoHttpClient httpClient;

        public YisoAuthService(YisoHttpClient httpClient) {
            this.httpClient = httpClient;
        }

        /// <summary>
        /// 회원가입
        /// </summary>
        public async Task<YisoHttpResponse<AuthResponse>> RegisterAsync(string username, string password) {
            var request = new RegisterRequest { Username = username, Password = password };
            return await httpClient.PostAsync<AuthResponse>(YisoApiEndpoints.Auth.Register, request);
        }

        /// <summary>
        /// 로그인
        /// </summary>
        public async Task<YisoHttpResponse<AuthResponse>> LoginAsync(string username, string password) {
            var request = new LoginRequest { Username = username, Password = password };
            return await httpClient.PostAsync<AuthResponse>(YisoApiEndpoints.Auth.Login, request);
        }

        /// <summary>
        /// 현재 사용자 정보 조회
        /// </summary>
        public async Task<YisoHttpResponse<YisoUserInfo>> GetCurrentUserAsync() {
            return await httpClient.GetAsync<YisoUserInfo>(YisoApiEndpoints.Auth.Me);
        }
    }
}
