using System;
using System.Threading.Tasks;
using Network.Web.Core;
using Network.Web.Features.Auth.DTOs;
using Utils;

namespace Network.Web.Features.Auth {
    /// <summary>
    /// 세션 상태 관리자
    /// 로그인 상태, 토큰 관리, 자동 로그인 등을 처리
    /// </summary>
    public class YisoSessionManager {
        private readonly YisoHttpClient httpClient;
        private readonly YisoAuthService authService;
        private readonly YisoTokenStorage tokenStorage;

        private bool isLoggedIn;
        private string currentUsername;
        
        public bool IsLoggedIn => isLoggedIn;
        public string CurrentUsername => currentUsername;
        
        public event Action<bool> OnLoginStateChanged;

        public YisoSessionManager(YisoHttpClient httpClient) {
            this.httpClient = httpClient;
            authService = new YisoAuthService(httpClient);
            tokenStorage = new YisoTokenStorage();
        }

        /// <summary>
        /// 저장된 토큰으로 자동 로그인 시도
        /// </summary>
        public async Task<bool> TryAutoLoginAsync() {
            if (!tokenStorage.HasToken()) {
                YisoLogger.Log("[YisoSession] 저장된 토큰 없음");
                return false;
            }

            if (tokenStorage.IsTokenExpired()) {
                YisoLogger.Log("[YisoSession] 토큰 만료됨");
                tokenStorage.ClearToken();
                return false;
            }

            var token = tokenStorage.LoadToken();
            httpClient.SetAuthToken(token);

            // 토큰 유효성 검증 (서버에 확인)
            // 헤더에 토큰 들어가 있음
            var response = await authService.GetCurrentUserAsync();

            if (response.IsSuccess) {
                isLoggedIn = true;
                currentUsername = response.Data.username;
                YisoLogger.Log($"[YisoSession] 자동 로그인 성공: {currentUsername}");
                OnLoginStateChanged?.Invoke(true);
                return true;
            }

            YisoLogger.Log($"[YisoSession] 자동 로그인 실패: {response.Error}");
            httpClient.ClearAuthToken();
            tokenStorage.ClearToken();
            return false;
        }

        /// <summary>
        /// 회원가입 후 자동 로그인
        /// </summary>
        public async Task<YisoHttpResponse<YisoAuthResponse>> RegisterAsync(string username, string password) {
            var response = await authService.RegisterAsync(username, password);

            if (response.IsSuccess) {
                HandleAuthSuccess(response.Data);
            }

            return response;
        }

        /// <summary>
        /// 로그인
        /// </summary>
        public async Task<YisoHttpResponse<YisoAuthResponse>> LoginAsync(string username, string password) {
            var response = await authService.LoginAsync(username, password);

            if (response.IsSuccess) {
                HandleAuthSuccess(response.Data);
            }

            return response;
        }

        /// <summary>
        /// 로그아웃
        /// </summary>
        public void Logout() {
            httpClient.ClearAuthToken();
            tokenStorage.ClearToken();  
            isLoggedIn = false;
            currentUsername = null;

            YisoLogger.Log("[YisoSession] 로그아웃됨");
            OnLoginStateChanged?.Invoke(false);
        }

        /// <summary>
        /// 현재 사용자 정보 조회
        /// </summary>
        public async Task<YisoHttpResponse<YisoUserInfo>> GetCurrentUserAsync() {
            return await authService.GetCurrentUserAsync();
        }

        private void HandleAuthSuccess(YisoAuthResponse authResponse) {
            var expiresAt = authResponse.GetExpiresAt();

            // 토큰 저장
            tokenStorage.SaveToken(authResponse.token, authResponse.username, expiresAt);

            // HTTP 클라이언트에 토큰 설정
            httpClient.SetAuthToken(authResponse.token);

            // 세션 상태 업데이트
            isLoggedIn = true;
            currentUsername = authResponse.username;

            YisoLogger.Log($"[YisoSession] 로그인 성공: {currentUsername}");
            OnLoginStateChanged?.Invoke(true);
        }
    }
}
