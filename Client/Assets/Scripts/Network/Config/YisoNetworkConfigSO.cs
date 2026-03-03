using UnityEngine;

namespace Network.Config {
    /// <summary>
    /// 네트워크 설정 ScriptableObject
    /// 환경별(개발/스테이징/프로덕션) 프리셋으로 관리
    /// </summary>
    [CreateAssetMenu(fileName = "NetworkConfig", menuName = "Yiso/Network/Config")]
    public class YisoNetworkConfigSO : ScriptableObject {
        [Header("웹 서버 (HTTP)")]
        [SerializeField] private string webServerHost = "127.0.0.1";
        [SerializeField] private int webServerPort = 5070;

        [Header("게임 서버 (TCP)")]
        [SerializeField] private string gameServerHost = "127.0.0.1";
        [SerializeField] private int gameServerPort = 7777;

        [Header("옵션")]
        [SerializeField] private bool autoLoginOnStart = true;
        [SerializeField] private int requestTimeoutSeconds = 30;

        public string WebServerUrl => $"http://{webServerHost}:{webServerPort}";
        public string GameServerUrl => $"http://{gameServerHost}:{gameServerPort}";
        public string WebServerHost => webServerHost;
        public int WebServerPort => webServerPort;
        public string GameServerHost => gameServerHost;
        public int GameServerPort => gameServerPort;
        public bool AutoLoginOnStart => autoLoginOnStart;
        public int RequestTimeoutSeconds => requestTimeoutSeconds;
    }
}
