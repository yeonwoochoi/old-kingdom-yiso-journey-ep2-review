using System;

namespace Network.Game {
    public abstract class NetworkEvent { }

    public sealed class PacketEvent : NetworkEvent {
        public ArraySegment<byte> segment;
    }

    public sealed class ConnectedEvent : NetworkEvent { }

    public sealed class DisconnectedEvent : NetworkEvent {
        public string Reason;
    }
}