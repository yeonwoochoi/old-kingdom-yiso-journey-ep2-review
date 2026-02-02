namespace Yiso.Web.DTOs;

/// <summary>
/// API 에러 응답 공통 형식
/// 미들웨어, 필터, 컨트롤러 모두 이 형식 사용
/// </summary>
public class ErrorResponse {
    public bool Success { get; init; } = false;
    public required string Message { get; init; }
    public int StatusCode { get; init; }

    public static ErrorResponse Create(string message, int statusCode) {
        return new ErrorResponse {
            Message = message,
            StatusCode = statusCode
        };
    }
}
