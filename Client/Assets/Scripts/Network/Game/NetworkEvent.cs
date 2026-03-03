namespace Network.Game {
    public abstract class NetworkEvent { }

    public sealed class PacketEvent : NetworkEvent {
        public YisoPacket Packet;
    }

    public sealed class ConnectedEvent : NetworkEvent { }

    public sealed class DisconnectedEvent : NetworkEvent {
        public string Reason;
    }
}