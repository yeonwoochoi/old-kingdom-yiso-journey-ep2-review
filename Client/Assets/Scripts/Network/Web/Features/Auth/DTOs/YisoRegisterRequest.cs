using System;

namespace Network.Web.Features.Auth.DTOs {
    /// <summary>
    /// 회원가입 요청 DTO
    /// </summary>
    [Serializable]
    public class YisoRegisterRequest {
        public string username;
        public string password;

        public YisoRegisterRequest(string username, string password) {
            this.username = username;
            this.password = password;
        }
    }
}
