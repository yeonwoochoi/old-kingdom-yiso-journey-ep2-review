using System;
using System.Threading.Tasks;
using Network.Web.Core;
using Network.Web.Features.Auth.DTOs;
using Utils;
using Yiso.Shared.DTOs.Auth;

namespace Network.Web.Features.Auth {
    /// <summary>
    /// 세션 상태 관리자
    /// 로그인 상태, 세션 관리, 자동 로그인 등을 처리
    /// </summary>
    public class YisoSessionManager {
        private readonly YisoHttpClient httpClient;
        private readonly YisoAuthService authService;
        private readonly YisoSessionStorage sessionStorage;

        private bool isLoggedIn;
        private string currentUsername;

        public bool IsLoggedIn => isLoggedIn;
        public string CurrentUsername => currentUsername;

        public event Action<bool> OnLoginStateChanged;

        public YisoSessionManager(YisoHttpClient httpClient) {
            this.httpClient = httpClient;
            authService = new YisoAuthService(httpClient);
            sessionStorage = new YisoSessionStorage();
        }

        /// <summary>
        /// 저장된 세션 ID로 자동 로그인 시도
        /// </summary>
        public async Task<bool> TryAutoLoginAsync() {
            if (!sessionStorage.HasSession()) {
                YisoLogger.Log("[YisoSession] 저장된 세션 없음");
                return false;
            }

            var sessionId = sessionStorage.LoadSessionId();
            httpClient.SetSessionId(sessionId);

            // 세션 유효성 검증 (서버에 확인)
            // 헤더에 세션 ID 들어가 있음
            var response = await authService.GetCurrentUserAsync();

            if (response.IsSuccess) {
                isLoggedIn = true;
                currentUsername = response.Data.Username;
                YisoLogger.Log($"[YisoSession] 자동 로그인 성공: {currentUsername}");
                OnLoginStateChanged?.Invoke(true);
                return true;
            }

            YisoLogger.Log($"[YisoSession] 자동 로그인 실패: {response.Error}");
            httpClient.ClearSessionId();
            sessionStorage.ClearSession();
            return false;
        }

        /// <summary>
        /// 회원가입 후 자동 로그인
        /// </summary>
        public async Task<YisoHttpResponse<AuthResponse>> RegisterAsync(string username, string password) {
            var response = await authService.RegisterAsync(username, password);

            if (response.IsSuccess) {
                HandleAuthSuccess(response.Data);
            }

            return response;
        }

        /// <summary>
        /// 로그인
        /// </summary>
        public async Task<YisoHttpResponse<AuthResponse>> LoginAsync(string username, string password) {
            var response = await authService.LoginAsync(username, password);

            if (response.IsSuccess) {
                HandleAuthSuccess(response.Data);
            }

            return response;
        }

        /// <summary>
        /// 로그아웃 (서버 세션 삭제 후 로컬 정리)
        /// </summary>
        public async Task LogoutAsync() {
            // 서버에 로그아웃 요청 (세션 삭제)
            var response = await httpClient.PostAsync(YisoApiEndpoints.Auth.Logout);
            if (!response.IsSuccess) {
                YisoLogger.Log($"[YisoSession] 서버 로그아웃 실패: {response.Error}");
            }

            httpClient.ClearSessionId();
            sessionStorage.ClearSession();
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

        private void HandleAuthSuccess(AuthResponse authResponse) {
            // 세션 저장
            sessionStorage.SaveSession(authResponse.SessionId, authResponse.Username);

            // HTTP 클라이언트에 세션 ID 설정
            httpClient.SetSessionId(authResponse.SessionId);

            // 세션 상태 업데이트
            isLoggedIn = true;
            currentUsername = authResponse.Username;

            YisoLogger.Log($"[YisoSession] 로그인 성공: {currentUsername}");
            OnLoginStateChanged?.Invoke(true);
        }
    }
}
