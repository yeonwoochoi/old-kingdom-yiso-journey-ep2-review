using Network.Config;
using Network.Game;
using Network.Web;
using UnityEngine;
using Utils;

namespace Network {
    /// <summary>
    /// 네트워크 최상위 싱글톤
    /// 웹서버(HTTP)과 게임서버(Socket) 통신을 총괄 관리
    /// </summary>
    public class YisoNetworkManager : MonoBehaviour {
        [Header("설정")]
        [SerializeField] private YisoNetworkConfigSO config;

        private static YisoNetworkManager instance;
        public static YisoNetworkManager Instance => instance;
        
        public YisoWebServerManager WebServer { get; private set; }
        public YisoGameServerManager GameServer { get; private set; }

        private void Awake() {
            if (instance != null && instance != this) {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);

            ValidateConfig();
            InitializeManagers();
        }

        private async void Start() {
            if (config.AutoLoginOnStart) {
                var success = await WebServer.Session.TryAutoLoginAsync();
                YisoLogger.Log($"[NetworkManager] 자동 로그인 결과: {success}");
            }
        }

        private void ValidateConfig() {
            if (config == null) {
                YisoLogger.LogError("[NetworkManager] Config가 할당되지 않음! Inspector에서 설정 필요");
            }
        }

        private void InitializeManagers() {
            WebServer = new YisoWebServerManager(config.WebServerUrl);
            GameServer = new YisoGameServerManager(config.GameServerUrl);
        }
    }
}
