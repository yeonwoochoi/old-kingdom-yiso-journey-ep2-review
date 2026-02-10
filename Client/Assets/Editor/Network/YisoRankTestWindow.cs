using System;
using System.Text;
using Network.Web;
using Network.Web.Features.Auth;
using Network.Web.Features.Rank;
using UnityEditor;
using UnityEngine;

namespace Editor.Network {
    /// <summary>
    /// 랭킹 테스트용 Editor Window
    /// Window > Yiso > Rank Test 에서 열기
    /// </summary>
    public class YisoRankTestWindow : EditorWindow {
        private string serverUrl = "http://localhost:5070";
        private string username = "testuser";
        private string password = "123123123";

        private int scoreToRegister = 1000;
        private int topCount = 10;

        private YisoWebManager webManager;
        private YisoSessionManager Session => webManager?.Session;
        private YisoRankService Rank => webManager?.Rank;

        private string logMessage = "";
        private Vector2 scrollPos;
        private bool isProcessing;

        [MenuItem("Yiso/Rank Test")]
        public static void ShowWindow() {
            var window = GetWindow<YisoRankTestWindow>("Rank Test");
            window.minSize = new Vector2(400, 550);
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

            // 인증 (로그인 필요)
            DrawAuthSection();

            EditorGUILayout.Space(10);

            // 랭킹 테스트
            DrawRankSection();

            EditorGUILayout.Space(10);

            // 로그
            DrawLogSection();
        }

        private void DrawStatusSection() {
            EditorGUILayout.LabelField("상태", EditorStyles.boldLabel);

            using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox)) {
                var status = Session?.IsLoggedIn == true
                    ? $"로그인됨 ({Session.CurrentUsername})"
                    : "로그아웃 상태";

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

        private void DrawAuthSection() {
            EditorGUILayout.LabelField("인증", EditorStyles.boldLabel);

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox)) {
                username = EditorGUILayout.TextField("사용자명", username);
                password = EditorGUILayout.PasswordField("비밀번호", password);

                EditorGUILayout.Space(5);

                GUI.enabled = !isProcessing;

                using (new EditorGUILayout.HorizontalScope()) {
                    if (GUILayout.Button("회원가입", GUILayout.Height(25))) {
                        RegisterAsync();
                    }

                    if (GUILayout.Button("로그인", GUILayout.Height(25))) {
                        LoginAsync();
                    }

                    if (GUILayout.Button("로그아웃", GUILayout.Height(25))) {
                        LogoutAsync();
                    }
                }

                GUI.enabled = true;
            }
        }

        private void DrawRankSection() {
            EditorGUILayout.LabelField("랭킹 테스트", EditorStyles.boldLabel);

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox)) {
                GUI.enabled = !isProcessing;

                // 점수 등록
                using (new EditorGUILayout.HorizontalScope()) {
                    scoreToRegister = EditorGUILayout.IntField("점수", scoreToRegister);

                    if (GUILayout.Button("점수 등록", GUILayout.Width(100), GUILayout.Height(20))) {
                        RegisterScoreAsync();
                    }
                }

                EditorGUILayout.Space(5);

                // Top N 조회
                using (new EditorGUILayout.HorizontalScope()) {
                    topCount = EditorGUILayout.IntField("조회 수", topCount);

                    if (GUILayout.Button("Top N 조회", GUILayout.Width(100), GUILayout.Height(20))) {
                        GetTopRanksAsync();
                    }
                }

                EditorGUILayout.Space(10);

                // 내 랭킹 조회 / 삭제
                using (new EditorGUILayout.HorizontalScope()) {
                    if (GUILayout.Button("내 랭킹 조회", GUILayout.Height(30))) {
                        GetMyRankAsync();
                    }

                    if (GUILayout.Button("내 랭킹 삭제", GUILayout.Height(30))) {
                        DeleteMyRankAsync();
                    }
                }

                GUI.enabled = true;
            }
        }

        private void DrawLogSection() {
            EditorGUILayout.LabelField("로그", EditorStyles.boldLabel);

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox, GUILayout.Height(150))) {
                scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

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

        #region Auth Actions

        private async void RegisterAsync() {
            if (Session == null) return;

            isProcessing = true;
            Log($"[요청] POST /auth/register\n- username: {username}");
            Repaint();

            try {
                var response = await Session.RegisterAsync(username, password);

                if (response.IsSuccess) {
                    Log($"[응답] 회원가입 성공\n- 상태: {response.StatusCode}\n- 사용자: {response.Data.Username}");
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
                    Log($"[응답] 로그인 성공\n- 상태: {response.StatusCode}\n- 사용자: {response.Data.Username}");
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
            Log("[요청] POST /auth/logout");
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

        #endregion

        #region Rank Actions

        private async void RegisterScoreAsync() {
            if (Rank == null) return;

            if (Session?.IsLoggedIn != true) {
                Log("[오류] 로그인이 필요합니다");
                return;
            }

            isProcessing = true;
            Log($"[요청] POST /rank/score\n- score: {scoreToRegister}");
            Repaint();

            try {
                var response = await Rank.RegisterScoreAsync(scoreToRegister);

                if (response.IsSuccess) {
                    Log($"[응답] 점수 등록 성공\n- 상태: {response.StatusCode}");
                }
                else {
                    Log($"[응답] 점수 등록 실패\n- 상태: {response.StatusCode}\n- 에러: {response.Error}");
                }
            }
            catch (Exception ex) {
                Log($"[에러] {ex.Message}");
            }

            isProcessing = false;
            Repaint();
        }

        private async void GetTopRanksAsync() {
            if (Rank == null) return;

            isProcessing = true;
            Log($"[요청] GET /rank/top?count={topCount}");
            Repaint();

            try {
                var response = await Rank.GetTopRanksAsync(topCount);

                if (response.IsSuccess) {
                    var sb = new StringBuilder();
                    sb.AppendLine($"[응답] Top {topCount} 랭킹 조회 성공");
                    sb.AppendLine($"- 상태: {response.StatusCode}");

                    if (response.Data.Ranks == null || response.Data.Ranks.Count == 0) {
                        sb.AppendLine("- (등록된 랭킹 없음)");
                    }
                    else {
                        foreach (var rank in response.Data.Ranks) {
                            sb.AppendLine($"  #{rank.Rank} {rank.Username}: {rank.Score}점");
                        }
                    }

                    Log(sb.ToString().TrimEnd());
                }
                else {
                    Log($"[응답] 랭킹 조회 실패\n- 상태: {response.StatusCode}\n- 에러: {response.Error}");
                }
            }
            catch (Exception ex) {
                Log($"[에러] {ex.Message}");
            }

            isProcessing = false;
            Repaint();
        }

        private async void GetMyRankAsync() {
            if (Rank == null) return;

            if (Session?.IsLoggedIn != true) {
                Log("[오류] 로그인이 필요합니다");
                return;
            }

            isProcessing = true;
            Log("[요청] GET /rank/me");
            Repaint();

            try {
                var response = await Rank.GetMyRankAsync();

                if (response.IsSuccess) {
                    Log($"[응답] 내 랭킹 조회 성공\n- 상태: {response.StatusCode}\n- 순위: #{response.Data.Rank}\n- 사용자: {response.Data.Username}\n- 점수: {response.Data.Score}점");
                }
                else {
                    Log($"[응답] 내 랭킹 조회 실패\n- 상태: {response.StatusCode}\n- 에러: {response.Error}");
                }
            }
            catch (Exception ex) {
                Log($"[에러] {ex.Message}");
            }

            isProcessing = false;
            Repaint();
        }

        private async void DeleteMyRankAsync() {
            if (Rank == null) return;

            if (Session?.IsLoggedIn != true) {
                Log("[오류] 로그인이 필요합니다");
                return;
            }

            isProcessing = true;
            Log("[요청] DELETE /rank/me");
            Repaint();

            try {
                var response = await Rank.DeleteMyRankAsync();

                if (response.IsSuccess) {
                    Log($"[응답] 내 랭킹 삭제 성공\n- 상태: {response.StatusCode}");
                }
                else {
                    Log($"[응답] 내 랭킹 삭제 실패\n- 상태: {response.StatusCode}\n- 에러: {response.Error}");
                }
            }
            catch (Exception ex) {
                Log($"[에러] {ex.Message}");
            }

            isProcessing = false;
            Repaint();
        }

        #endregion

        private void Log(string message) {
            var timestamp = DateTime.Now.ToString("HH:mm:ss");
            logMessage = $"[{timestamp}] {message}\n\n{logMessage}";
        }
    }
}
