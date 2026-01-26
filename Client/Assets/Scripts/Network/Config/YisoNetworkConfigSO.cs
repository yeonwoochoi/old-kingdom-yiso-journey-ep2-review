using UnityEngine;

namespace Network.Config {
    /// <summary>
    /// 네트워크 설정 ScriptableObject
    /// 환경별(개발/스테이징/프로덕션) 프리셋으로 관리
    /// </summary>
    [CreateAssetMenu(fileName = "NetworkConfig", menuName = "Yiso/Network/Config")]
    public class YisoNetworkConfigSO : ScriptableObject {
        [Header("웹 서버 (HTTP)")]
        [SerializeField] private string webServerUrl = "http://localhost:5070";

        [Header("게임 서버 (Socket)")]
        [SerializeField] private string socketServerUrl = "ws://localhost:5071";

        [Header("옵션")]
        [SerializeField] private bool autoLoginOnStart = true;
        [SerializeField] private int requestTimeoutSeconds = 30;

        public string WebServerUrl => webServerUrl;
        public string SocketServerUrl => socketServerUrl;
        public bool AutoLoginOnStart => autoLoginOnStart;
        public int RequestTimeoutSeconds => requestTimeoutSeconds;
    }
}
