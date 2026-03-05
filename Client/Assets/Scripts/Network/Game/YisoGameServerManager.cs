namespace Network.Game {
    /// <summary>
    /// 게임(소켓) 서버 연결 담당 매니저
    /// 실시간 통신(채팅, 멀티플레이 등)에 사용
    /// </summary>
    public class YisoGameServerManager {
        private readonly string serverUrl;
        private bool isConnected;
        private YisoPacketDispatcher _dispatcher;
        private YisoTcpHandler _tcpHandler;
        
        public YisoGameServerManager(string serverUrl) {
            this.serverUrl = serverUrl;
            isConnected = false;

            _tcpHandler = new YisoTcpHandler();
            _dispatcher = new YisoPacketDispatcher();
            
            // TODO: 여기서 _dispatcher.RegisterHandlerSource 해줘야함.
            // _dispatcher.RegisterHandlerSource(WorldManager.Instance);
            
            _tcpHandler.OnRecv += _dispatcher.OnRecv;
            _tcpHandler.OnConnected += _dispatcher.OnConnected;
            _tcpHandler.OnDisconnected += _dispatcher.OnDisconnected;
        }
    }
}
