using System;
using MemoryPack;

namespace ServerShared.Models {
    /// <summary>
    /// Redis에 저장되는 세션 데이터
    /// SessionId는 해당 SessionData를 찾아오는 키로 사용될 것이기 때문에 여기에 포함 안됨!!!!
    /// </summary>
    [MemoryPackable]
    public partial class SessionData {
        public string UserId { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime LastAccessedAt { get; set; } = DateTime.UtcNow;
    }
}
