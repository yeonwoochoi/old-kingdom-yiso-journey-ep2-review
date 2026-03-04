using System;
using System.Collections.Generic;
using System.Reflection;
using Google.Protobuf;
using Utils;
using Yiso.Game;

namespace Network.Game {
    [AttributeUsage(AttributeTargets.Method)]
    public class PacketHandlerAttribute : Attribute {
        public PacketType Type { get; }
        public PacketHandlerAttribute(PacketType type) => Type = type;
    }

    public class YisoPacketDispatcher {
        private Dictionary<PacketType, Action<byte[]>> _handlers = new();
        private MethodInfo _registerMethod;

        public YisoPacketDispatcher() {
            _registerMethod = typeof(YisoPacketDispatcher).GetMethod(nameof(RegisterHandler),
                BindingFlags.NonPublic | BindingFlags.Instance);
        }

        public void RegisterHandlerSource(object source) {
            var methods = source.GetType().GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
            foreach (var method in methods) {
                if (!method.IsPrivate) continue;
                var attr = method.GetCustomAttribute<PacketHandlerAttribute>();
                if (attr == null) continue;

                var paramType = method.GetParameters()[0].ParameterType; // S2C_Chat 등
                _registerMethod!.MakeGenericMethod(paramType).Invoke(this, new object[] {source, attr.Type, method});
            }
        }

        private void RegisterHandler<T>(object source, PacketType type, MethodInfo method) where T : IMessage<T> {
            var parser = (MessageParser<T>) typeof(T).GetProperty("Parser")!.GetValue(null);
            var handler = (Action<T>) Delegate.CreateDelegate(typeof(Action<T>), source, method);
            _handlers[type] = payload => handler(parser.ParseFrom(payload));
        }

        public int OnRecv(ArraySegment<byte> segment) {
            var consumed = 0;
            while (YisoPacketParser.TryDecode(ref segment, out var packet)) {
                var packetType = (PacketType) packet.type;
                if (Enum.IsDefined(typeof(PacketType), packetType) == false) {
                    consumed += packet.GetSize();
                    continue;
                }

                if (_handlers.TryGetValue(packetType, out var handler))
                    handler(packet.payload);
                else
                    YisoLogger.LogWarning($"[Dispatcher] 핸들러 없음: {packetType}");
                consumed += packet.GetSize();
            }

            return consumed;
        }

        public void OnConnected() {
        }

        public void OnDisconnected() {
        }
    }
}