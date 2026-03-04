using System;
using Google.Protobuf;

namespace Network.Game {
    /// <summary>
    /// [ body_size: 4 bytes ][ packet_type: 2 bytes ][ payload: body_size bytes ]
    /// </summary>
    public class YisoPacket {
        public uint size;
        public ushort type;
        public byte[] payload;

        public int GetSize() {
            return YisoPacketParser.HEADER_SIZE + (int)size;
        }
    }

    public static class YisoPacketParser {
        public const int HEADER_SIZE = 6;
        
        public static byte[] Encode(this IMessage message, ushort type) {
            byte[] payload = message.ToByteArray();
            byte[] frame = new byte[HEADER_SIZE + payload.Length];
                
            BitConverter.GetBytes((uint)payload.Length).CopyTo(frame, 0);
            BitConverter.GetBytes((ushort)type).CopyTo(frame, 4);
            Buffer.BlockCopy(payload, 0, frame, HEADER_SIZE, payload.Length);
            
            return frame;
        }

        public static T Decode<T>(this byte[] payload) where T : IMessage<T> {
            return (T)typeof(T).GetProperty("Parser")!.GetValue(null) is MessageParser<T> parser 
                ? parser.ParseFrom(payload)
                : throw new InvalidOperationException();
        }

        public static bool TryDecode(ref ArraySegment<byte> data, out YisoPacket packet) {
            packet = null;
            if (data.Count < HEADER_SIZE) {
                return false;
            }

            byte[] buffer = data.Array;
            int offset = data.Offset;

            uint size = BitConverter.ToUInt32(buffer, offset);
            ushort type = BitConverter.ToUInt16(buffer, offset + 4);
            
            int packetSize = HEADER_SIZE + (int)size;
            if (data.Count < packetSize)
                return false;
            
            byte[] payload = new byte[size];
            Buffer.BlockCopy(buffer, offset + HEADER_SIZE, payload, 0, (int)size);
            packet = new YisoPacket {
                size = size,
                type = type,
                payload = payload
            };
            
            data = new ArraySegment<byte>(buffer, offset + packetSize, data.Count - packetSize);
            return true;
        }
    }
}