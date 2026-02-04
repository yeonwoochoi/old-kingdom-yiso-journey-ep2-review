using System;

namespace Network.Web.Features.Auth.DTOs {
    /// <summary>
    /// 인증 응답 DTO
    /// 서버의 AuthResponse와 매칭됨
    /// </summary>
    [Serializable]
    public class YisoAuthResponse {
        public string sessionId;
        public string username;
    }

    /// <summary>
    /// 사용자 정보 DTO
    /// /api/auth/me 응답이랑 동일함
    /// </summary>
    [Serializable]
    public class YisoUserInfo {
        public string id;
        public string username;
        public string createdAt;
        public string lastAccessedAt;

        /// <summary>
        /// 계정 생성 시간을 DateTime으로 파싱
        /// </summary>
        public DateTime GetCreatedAt() {
            if (DateTime.TryParse(createdAt, out var result)) {
                return result;
            }
            return DateTime.MinValue;
        }

        /// <summary>
        /// 마지막 접속 시간을 DateTime으로 파싱
        /// </summary>
        public DateTime GetLastAccessedAt() {
            if (DateTime.TryParse(lastAccessedAt, out var result)) {
                return result;
            }
            return DateTime.MinValue;
        }
    }
}
