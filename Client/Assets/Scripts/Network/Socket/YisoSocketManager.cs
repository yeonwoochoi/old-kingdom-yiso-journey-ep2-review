using System;
using UnityEngine;
using Utils;

namespace Network.Socket {
    /// <summary>
    /// 게임(소켓) 서버 연결 담당 매니저
    /// 실시간 통신(채팅, 멀티플레이 등)에 사용
    /// </summary>
    public class YisoSocketManager {
        private readonly string serverUrl;
        private bool isConnected;

        /// <summary>
        /// 연결 상태
        /// </summary>
        public bool IsConnected => isConnected;

        /// <summary>
        /// 연결 상태 변경 이벤트
        /// </summary>
        public event Action<bool> OnConnectionStateChanged;

        /// <summary>
        /// 메시지 수신 이벤트
        /// </summary>
        public event Action<string> OnMessageReceived;

        public YisoSocketManager(string serverUrl) {
            this.serverUrl = serverUrl;
            isConnected = false;
        }
        
        public void Connect() {
            // TODO: 실제 WebSocket 연결 구현해야함..
            YisoLogger.Log($"[YisoSocket] 연결 시도: {serverUrl}");

            // 임시: 연결 성공으로 가정
            isConnected = true;
            OnConnectionStateChanged?.Invoke(true);
        }
        
        public void Disconnect() {
            // TODO: 실제 연결 해제 구현해야함..
            YisoLogger.Log("[YisoSocket] 연결 해제");

            isConnected = false;
            OnConnectionStateChanged?.Invoke(false);
        }
        
        public void Send(string message) {
            if (!isConnected) {
                YisoLogger.LogWarning("[YisoSocket] 연결되지 않은 상태에서 전송 시도");
                return;
            }

            // TODO: 실제 메시지 전송 구현해야함..
            YisoLogger.Log($"[YisoSocket] 전송: {message}");
        }
        
        /// <summary>
        /// JSON 형식으로 전송
        /// </summary>
        /// <param name="data"></param>
        /// <typeparam name="T"></typeparam>
        public void Send<T>(T data) {
            var json = JsonUtility.ToJson(data);
            Send(json);
        }
    }
}
