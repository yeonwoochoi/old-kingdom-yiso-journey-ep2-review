using Network.Web.Core;
using Network.Web.Features.Auth;
using Network.Web.Features.Rank;

namespace Network.Web {
    /// <summary>
    /// 웹 서버 통신 총괄
    /// HTTP 통신 관련해서 단일 진입점
    /// </summary>
    public class YisoWebManager {
        private readonly YisoHttpClient httpClient; // HttpClient 인스턴스를 하나 생성해 하위 매니저들에게 주입
        
        public YisoHttpClient HttpClient => httpClient;
        
        #region Managers

        // 매니저(들)
        public YisoSessionManager Session { get; } // 인증 매니저
        public YisoRankService Rank { get; } // 랭킹 서비스
        
        #endregion
        
        public YisoWebManager(string serverUrl) {
            httpClient = new YisoHttpClient(serverUrl);
            Session = new YisoSessionManager(httpClient);
            Rank = new YisoRankService(httpClient);
        }
    }
}
