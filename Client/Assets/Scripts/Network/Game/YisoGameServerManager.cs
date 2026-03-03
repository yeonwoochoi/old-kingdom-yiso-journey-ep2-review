namespace Network.Game {
    /// <summary>
    /// 게임(소켓) 서버 연결 담당 매니저
    /// 실시간 통신(채팅, 멀티플레이 등)에 사용
    /// </summary>
    public class YisoGameServerManager {
        private readonly string serverUrl;
        private bool isConnected;
        
        public YisoGameServerManager(string serverUrl) {
            this.serverUrl = serverUrl;
            isConnected = false;
        }
    }
}
