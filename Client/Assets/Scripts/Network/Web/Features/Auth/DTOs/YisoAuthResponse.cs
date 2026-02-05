using System;

namespace Network.Web.Features.Auth.DTOs {
    /// <summary>
    /// 사용자 정보 DTO
    /// /api/auth/me 응답용 (클라이언트 전용)
    /// </summary>
    public class YisoUserInfo {
        public string UserId { get; set; }
        public string Username { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime LastAccessedAt { get; set; }
    }
}
