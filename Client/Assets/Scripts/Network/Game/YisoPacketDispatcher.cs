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
        
        public YisoPacketDispatcher(object[] sources) {
            RegisterHandlers(sources);
        }

        private void RegisterHandlers(object[] sources) {
            if (sources == null) return;
            foreach (var source in sources) {
                var methods = source.GetType().GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                foreach (var method in methods) {
                    if (!method.IsPrivate) continue;
                    var attr = method.GetCustomAttribute<PacketHandlerAttribute>();
                    if (attr == null) continue;
                    
                    var handler = (Action<byte[]>)Delegate.CreateDelegate(typeof(Action<byte[]>), source, method);
                    _handlers[attr.Type] = handler;
                }
            }
        }

        public int OnRecv(ArraySegment<byte> segment) {
            var consumed = 0;
            while (YisoPacketParser.TryDecode(ref segment, out var packet)) {
                var packetType = (PacketType)packet.type;
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