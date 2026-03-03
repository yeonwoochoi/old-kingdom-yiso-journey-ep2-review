using System;
using Utils;

namespace Network.Game {
    /// <summary>
    /// 버퍼 안에 처리 대기 중인 데이터를 ArraySegment로 노출하기만 하는 역할
    /// 파싱은 여기서 하지 않음
    /// </summary>
    public class RecvBuffer {
        private ArraySegment<byte> _buffer;
        
        private int _readPos = 0;
        private int _writePos = 0;
        
        public RecvBuffer(int bufferSize) {
            _buffer = new ArraySegment<byte>(new byte[bufferSize], 0, bufferSize);
        }

        public int DataSize => _writePos - _readPos;
        public int FreeSize => _buffer.Count - _writePos;

        public ArraySegment<byte> ReadSegment => new ArraySegment<byte>(_buffer.Array, _buffer.Offset + _readPos, DataSize);
        public ArraySegment<byte> WriteSegment => new ArraySegment<byte>(_buffer.Array, _buffer.Offset + _writePos, FreeSize);
        
        public bool OnRead(int numOfBytes) {
            if (numOfBytes > DataSize) {
                return false;
            }
            _readPos += numOfBytes;
            return true;
        }
        
        public bool OnWrite(int numOfBytes) {
            if (numOfBytes > _buffer.Count) {
                return false;
            }
            
            if (numOfBytes > FreeSize) {
                Clean();
                if (numOfBytes > FreeSize) {
                    return false;
                }
            }
            
            _writePos += numOfBytes;
            return true;
        }

        private void Clean() {
            var dataSize = DataSize;
            if (_readPos == 0) {
                return;
            }
            Buffer.BlockCopy(_buffer.Array, _buffer.Offset + _readPos, _buffer.Array, _buffer.Offset, dataSize);
            _readPos = 0;
            _writePos = dataSize;
        }
    }
}