using System;

namespace Network.Web.Features.Auth.DTOs {
    /// <summary>
    /// 인증 응답 DTO
    /// 서버의 AuthResponse와 매칭됨
    /// </summary>
    [Serializable]
    public class YisoAuthResponse {
        public string token;
        public string username;
        public string expiresAt; // JsonUtility가 DateTime을 직접 지원하지 않아서 string 사용하는거

        /// <summary>
        /// string인 토큰 만료 시간(expiredAt)을 DateTime으로 파싱하기 위한 메서드
        /// </summary>
        public DateTime GetExpiresAt() {
            if (DateTime.TryParse(expiresAt, out var result)) {
                return result;
            }
            return DateTime.MinValue;
        }
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

        /// <summary>
        /// 계정 생성 시간을 DateTime으로 파싱
        /// </summary>
        public DateTime GetCreatedAt() {
            if (DateTime.TryParse(createdAt, out var result)) {
                return result;
            }
            return DateTime.MinValue;
        }
    }
}
