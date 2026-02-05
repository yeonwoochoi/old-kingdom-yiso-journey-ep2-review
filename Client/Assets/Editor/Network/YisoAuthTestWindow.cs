using System;
using Network.Web;
using Network.Web.Features.Auth;
using UnityEditor;
using UnityEngine;

namespace Editor.Network {
    /// <summary>
    /// 인증 테스트용 Editor Window
    /// Window > Yiso > Auth Test 에서 열기
    /// </summary>
    public class YisoAuthTestWindow : EditorWindow {
        private string serverUrl = "http://localhost:5070";
        private string username = "testuser";
        private string password = "testpass123";

        private YisoWebManager webManager;
        private YisoSessionManager Session => webManager?.Session;

        private string logMessage = "";
        private Vector2 scrollPos;
        private bool isProcessing;

        [MenuItem("Yiso/Auth Test")]
        public static void ShowWindow() {
            var window = GetWindow<YisoAuthTestWindow>("Auth Test");
            window.minSize = new Vector2(350, 400);
        }

        private void OnEnable() {
            InitializeWebManager();
        }

        private void InitializeWebManager() {
            webManager = new YisoWebManager(serverUrl);
            if (Session != null) {
                Session.OnLoginStateChanged += OnLoginStateChanged;
            }
        }

        private void OnDisable() {
            if (Session != null) {
                Session.OnLoginStateChanged -= OnLoginStateChanged;
            }
        }

        private void OnLoginStateChanged(bool isLoggedIn) {
            Log(isLoggedIn
                ? $"[상태 변경] 로그인 -> {Session.CurrentUsername}"
                : "[상태 변경] 로그아웃");
            Repaint();
        }

        private void OnGUI() {
            EditorGUILayout.Space(10);

            // 상태 표시
            DrawStatusSection();

            EditorGUILayout.Space(10);

            // 서버 설정
            DrawServerSection();

            EditorGUILayout.Space(10);

            // 계정 정보
            DrawAccountSection();

            EditorGUILayout.Space(10);

            // 버튼들
            DrawActionButtons();

            EditorGUILayout.Space(10);

            // 로그
            DrawLogSection();
        }

        private void DrawStatusSection() {
            EditorGUILayout.LabelField("상태", EditorStyles.boldLabel);

            using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox)) {
                var status = Session?.IsLoggedIn == true
                    ? $"✅ 로그인됨 ({Session.CurrentUsername})"
                    : "❌ 로그아웃 상태";

                EditorGUILayout.LabelField(status);
            }
        }

        private void DrawServerSection() {
            EditorGUILayout.LabelField("서버 설정", EditorStyles.boldLabel);

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox)) {
                serverUrl = EditorGUILayout.TextField("URL", serverUrl);

                if (GUILayout.Button("서버 URL 적용")) {
                    InitializeWebManager();
                    Log($"[설정] 서버 URL 변경: {serverUrl}");
                    Repaint();
                }
            }
        }

        private void DrawAccountSection() {
            EditorGUILayout.LabelField("계정 정보", EditorStyles.boldLabel);

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox)) {
                username = EditorGUILayout.TextField("사용자명", username);
                password = EditorGUILayout.PasswordField("비밀번호", password);
            }
        }

        private void DrawActionButtons() {
            EditorGUILayout.LabelField("액션", EditorStyles.boldLabel);

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox)) {
                GUI.enabled = !isProcessing;

                using (new EditorGUILayout.HorizontalScope()) {
                    if (GUILayout.Button("회원가입", GUILayout.Height(30))) {
                        RegisterAsync();
                    }

                    if (GUILayout.Button("로그인", GUILayout.Height(30))) {
                        LoginAsync();
                    }
                }

                using (new EditorGUILayout.HorizontalScope()) {
                    if (GUILayout.Button("자동 로그인", GUILayout.Height(30))) {
                        TryAutoLoginAsync();
                    }

                    if (GUILayout.Button("로그아웃", GUILayout.Height(30))) {
                        LogoutAsync();
                    }
                }

                EditorGUILayout.Space(5);

                if (GUILayout.Button("내 정보 조회", GUILayout.Height(25))) {
                    GetCurrentUserAsync();
                }

                GUI.enabled = true;
            }
        }

        private void DrawLogSection() {
            EditorGUILayout.LabelField("로그", EditorStyles.boldLabel);

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox, GUILayout.Height(150))) {
                scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

                // readonly 스타일로 로그 표시
                var style = new GUIStyle(EditorStyles.textArea) {
                    wordWrap = true,
                    richText = false
                };
                GUI.enabled = false;
                EditorGUILayout.TextArea(logMessage, style, GUILayout.ExpandHeight(true));
                GUI.enabled = true;

                EditorGUILayout.EndScrollView();
            }

            if (GUILayout.Button("로그 지우기")) {
                logMessage = "";
                Repaint();
            }
        }

        private async void RegisterAsync() {
            if (Session == null) return;

            isProcessing = true;
            Log($"[요청] POST /auth/register\n- username: {username}");
            Repaint();

            try {
                var response = await Session.RegisterAsync(username, password);

                if (response.IsSuccess) {
                    var sessionPreview = response.Data.SessionId.Length > 20
                        ? response.Data.SessionId[..20] + "..."
                        : response.Data.SessionId;
                    Log($"[응답] 회원가입 성공\n- 상태: {response.StatusCode}\n- 사용자: {response.Data.Username}\n- 세션 ID: {sessionPreview}");
                }
                else {
                    Log($"[응답] 회원가입 실패\n- 상태: {response.StatusCode}\n- 에러: {response.Error}");
                }
            }
            catch (Exception ex) {
                Log($"[에러] {ex.Message}");
            }

            isProcessing = false;
            Repaint();
        }

        private async void LoginAsync() {
            if (Session == null) return;

            isProcessing = true;
            Log($"[요청] POST /auth/login\n- username: {username}");
            Repaint();

            try {
                var response = await Session.LoginAsync(username, password);

                if (response.IsSuccess) {
                    var sessionPreview = response.Data.SessionId.Length > 20
                        ? response.Data.SessionId[..20] + "..."
                        : response.Data.SessionId;
                    Log($"[응답] 로그인 성공\n- 상태: {response.StatusCode}\n- 사용자: {response.Data.Username}\n- 세션 ID: {sessionPreview}");
                }
                else {
                    Log($"[응답] 로그인 실패\n- 상태: {response.StatusCode}\n- 에러: {response.Error}");
                }
            }
            catch (Exception ex) {
                Log($"[에러] {ex.Message}");
            }

            isProcessing = false;
            Repaint();
        }

        private async void LogoutAsync() {
            if (Session == null) return;

            isProcessing = true;
            Log($"[요청] POST /auth/logout");
            Repaint();

            try {
                var response = await Session.LogoutAsync();

                if (response.IsSuccess) {
                    Log($"[응답] 로그아웃 성공\n- 상태: {response.StatusCode}");
                }
                else {
                    Log($"[응답] 로그아웃 실패\n- 상태: {response.StatusCode}\n- 에러: {response.Error}");
                }
            }
            catch (Exception ex) {
                Log($"[에러] {ex.Message}");
            }

            isProcessing = false;
            Repaint();
        }

        private async void TryAutoLoginAsync() {
            if (Session == null) return;

            isProcessing = true;
            Log("[요청] 자동 로그인 시도 (저장된 세션 확인)");
            Repaint();

            try {
                var success = await Session.TryAutoLoginAsync();

                if (success) {
                    Log($"[응답] 자동 로그인 성공\n- 사용자: {Session.CurrentUsername}");
                }
                else {
                    Log("[응답] 자동 로그인 실패\n- 사유: 저장된 세션 없음 또는 세션 만료");
                }
            }
            catch (Exception ex) {
                Log($"[에러] {ex.Message}");
            }

            isProcessing = false;
            Repaint();
        }

        private async void GetCurrentUserAsync() {
            if (Session == null) return;

            if (!Session.IsLoggedIn) {
                Log("[오류] 로그인이 필요합니다");
                return;
            }

            isProcessing = true;
            Log("[요청] GET /auth/me");
            Repaint();

            try {
                var response = await Session.GetCurrentUserAsync();

                if (response.IsSuccess) {
                    Log($"[응답] 사용자 정보 조회 성공\n- 상태: {response.StatusCode}\n- ID: {response.Data.UserId}\n- 이름: {response.Data.Username}\n- 생성일: {response.Data.CreatedAt}\n- 마지막 접속: {response.Data.LastAccessedAt}");
                }
                else {
                    Log($"[응답] 사용자 정보 조회 실패\n- 상태: {response.StatusCode}\n- 에러: {response.Error}");
                }
            }
            catch (Exception ex) {
                Log($"[에러] {ex.Message}");
            }

            isProcessing = false;
            Repaint();
        }

        private void Log(string message) {
            var timestamp = DateTime.Now.ToString("HH:mm:ss");
            logMessage = $"[{timestamp}] {message}\n\n{logMessage}";
        }
    }
}
