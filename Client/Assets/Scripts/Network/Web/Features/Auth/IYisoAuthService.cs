using System.Threading.Tasks;
using Network.Web.Core;
using Network.Web.Features.Auth.DTOs;

namespace Network.Web.Features.Auth {
    /// <summary>
    /// 인증 API 호출 서비스
    /// </summary>
    public interface IYisoAuthService {
        /// <summary>
        /// 회원가입
        /// </summary>
        Task<YisoHttpResponse<YisoAuthResponse>> RegisterAsync(string username, string password);

        /// <summary>
        /// 로그인
        /// </summary>
        Task<YisoHttpResponse<YisoAuthResponse>> LoginAsync(string username, string password);

        /// <summary>
        /// 현재 사용자 정보 조회 (인증 토큰 필요)
        /// </summary>
        Task<YisoHttpResponse<YisoUserInfo>> GetCurrentUserAsync();
    }
}
