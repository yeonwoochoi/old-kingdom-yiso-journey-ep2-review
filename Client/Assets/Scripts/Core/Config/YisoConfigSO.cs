using UnityEngine;

namespace Core.Config {
    public enum AppEnvironment {
        Dev,
        Stage,
        Live
    }
    
    /// <summary>
    /// [역할] 빌드에 구워지는 정적 환경 설정 데이터
    /// [책임]
    ///   - 서버 환경 (Dev / Stage / Live) 및 URL 보관
    ///   - 볼륨·언어 등 앱 기본값 보관
    ///   - 디버그 플래그 보관
    /// </summary>
    [CreateAssetMenu(fileName = "ConfigSO", menuName = "Yiso/Config")]
    public class YisoConfigSO : ScriptableObject {
        [Header("Environment")]
        public AppEnvironment environment;
        
        [Header("Web Server")]
        public string webServerBaseUrl;
        
        [Header("Game Server")]
        public string gameServerHost;
        public int gameServerPort;

        [Header("Default App Settings")]
        public float defaultBgmVolume = 1f;
        public float defaultSfxVolume = 1f;
        public SystemLanguage defaultLanguage = SystemLanguage.Korean;

        [Header("Debug")]
        public bool enableDebugLog = false;
        public bool skipLogin = false;
    }
}