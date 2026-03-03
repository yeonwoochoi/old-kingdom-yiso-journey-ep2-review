using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Utils;

namespace Network.Game {
    public enum NetworkStatus {
        None = 0,
        Connecting = 1,
        Connected = 2,
        Disconnecting = 3,
        Disconnected = 4
    }
    
    public class YisoTcpHandler {
        private Socket _socket;
        private NetworkStatus _status = NetworkStatus.None;
        private const int MAX_PACKET_SIZE = 64 * 1024; // 64KB (서버랑 맞춤)

        public event Func<ArraySegment<byte>, int> OnRecv;
        public NetworkStatus Status => _status;
        
        private RecvBuffer _recvBuffer;
        private SendBuffer _sendBuffer;

        public async Task ConnectAsync(EndPoint endPoint) {
            if (_status != NetworkStatus.None) {
                YisoLogger.LogWarning($"[TcpHandler] 이미 연결 중이거나 연결된 상태입니다: {endPoint}");
                return;
            }
            try
            {
                _socket = new Socket(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                _recvBuffer = new RecvBuffer(MAX_PACKET_SIZE * 4); // 256KB
                _sendBuffer = new SendBuffer(MAX_PACKET_SIZE);
                _status = NetworkStatus.Connecting;
                await _socket.ConnectAsync(endPoint);
                YisoLogger.Log("[TcpHandler] 서버 연결 성공");
                _status = NetworkStatus.Connected;
                _ = RecvLoop();
            }
            catch (SocketException ex)
            {
                YisoLogger.LogError($"[TcpHandler] 연결 실패: {ex.SocketErrorCode}");
                _status = NetworkStatus.None;
            }
        }

        public void Disconnect() {
            if (_status != NetworkStatus.Connected) {
                YisoLogger.LogWarning("[TcpHandler] 연결 해제 실패: 연결 상태가 아닙니다");
                return;
            }
            _status =  NetworkStatus.Disconnecting;
            _socket.Shutdown(SocketShutdown.Both);
            _socket.Close();
            _status =  NetworkStatus.Disconnected;
        }

        private async Task RecvLoop() {
            while (_status == NetworkStatus.Connected) {
                var segment = _recvBuffer.WriteSegment;
                var recvSize = await _socket.ReceiveAsync(segment, SocketFlags.None);
                if (recvSize <= 0) {
                    Disconnect();
                    return;
                }

                if (_recvBuffer.OnWrite(recvSize) == false) {
                    Disconnect();
                    return;
                }
                
                var consumed = OnRecv?.Invoke(_recvBuffer.ReadSegment) ?? 0;
                if (_recvBuffer.OnRead(consumed) == false) {
                    Disconnect();
                    return;
                }
            }
        }

        public async Task SendAsync(byte[] data) {
            if (data.Length == 0) {
                YisoLogger.LogWarning("[TcpHandler] 빈 데이터는 전송할 수 없습니다");
                return;
            }
            if (data.Length > MAX_PACKET_SIZE) {
                YisoLogger.LogWarning($"[TcpHandler] 패킷 크기({data.Length}byte)가 최대 크기({MAX_PACKET_SIZE}byte)를 초과합니다");
                return;
            }

            if (data.Length > _sendBuffer.FreeSize) {
                _sendBuffer.Clean();
                if (data.Length > _sendBuffer.FreeSize) {
                    YisoLogger.LogWarning("[TcpHandler] 버퍼 정리 후에도 공간이 부족합니다");
                    return;
                }
            }
            
            var segment = _sendBuffer.Open(data.Length);
            Buffer.BlockCopy(data, 0, segment.Array, segment.Offset, data.Length);
            await _socket.SendAsync(segment, SocketFlags.None);
            _sendBuffer.Close(data.Length);
        }
    }
}
