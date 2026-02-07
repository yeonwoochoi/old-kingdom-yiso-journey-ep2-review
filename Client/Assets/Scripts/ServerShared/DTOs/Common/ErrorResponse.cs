namespace ServerShared.DTOs.Common {
    /// <summary>
    /// API 에러 응답 공통 형식
    /// </summary>
    public class ErrorResponse {
        public bool Success { get; set; } = false;
        public string Message { get; set; } = string.Empty;
        public int StatusCode { get; set; }

        public static ErrorResponse Create(string message, int statusCode) {
            return new ErrorResponse {
                Message = message,
                StatusCode = statusCode
            };
        }
    }
}
