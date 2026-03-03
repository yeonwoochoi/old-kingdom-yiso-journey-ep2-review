using System;

namespace Network.Game {
    /// <summary>
    /// RecvBuffer랑 다르게 직렬화 스크래치 버퍼로 사용
    /// 즉, 매번 Send할 때마다 byte[]를 생성하기 보단 큰 사이즈의 buffer 생성한 후 일부분 사용
    /// RecvBuffer와 다르게 overflow 체크는 외부에서 함.
    /// </summary>
    public class SendBuffer {
        private byte[] _buffer;
        
        private int _usedSize = 0;
        public int FreeSize => _buffer.Length - _usedSize;

        public SendBuffer(int chunkSize) {
            _buffer = new byte[chunkSize];
        }

        public ArraySegment<byte> Open(int reserveSize) {
            if (reserveSize > FreeSize)
                throw new InvalidOperationException("SendBuffer에 공간이 부족합니다.");
            return new ArraySegment<byte>(_buffer, _usedSize, reserveSize);
        }

        public ArraySegment<byte> Close(int usedSize) {
            var segment = new ArraySegment<byte>(_buffer, _usedSize, usedSize);
            _usedSize += usedSize;
            return segment;
        }

        public void Clean() {
            _usedSize = 0;
        }
    }
}