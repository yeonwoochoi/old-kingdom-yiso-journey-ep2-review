namespace Network.Web.Core {
    /// <summary>
    /// API 엔드포인트 경로 상수
    /// 서버 API 변경 시 이 파일만 수정하면 됨
    /// </summary>
    public static class YisoApiEndpoints {
        /// <summary>
        /// 인증 관련 API
        /// </summary>
        public static class Auth {
            public const string Register = "api/auth/register";
            public const string Login = "api/auth/login";
            public const string Me = "api/auth/me";
            public const string Logout = "api/auth/logout";
        }

        // 향후 추가될 API들
        // public static class User {
        //     public const string Profile = "api/user/profile";
        //     public const string Update = "api/user/update";
        // }
        //
        // public static class Inventory {
        //     public const string List = "api/inventory";
        //     public const string Add = "api/inventory/add";
        // }
    }
}
