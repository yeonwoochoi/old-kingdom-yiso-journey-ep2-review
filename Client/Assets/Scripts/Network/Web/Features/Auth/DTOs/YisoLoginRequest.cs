using System;

namespace Network.Web.Features.Auth.DTOs {
    /// <summary>
    /// 로그인 요청 DTO
    /// </summary>
    [Serializable]
    public class YisoLoginRequest {
        public string username;
        public string password;

        public YisoLoginRequest(string username, string password) {
            this.username = username;
            this.password = password;
        }
    }
}
