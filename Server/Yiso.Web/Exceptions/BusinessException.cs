namespace Yiso.Web.Exceptions;

/// <summary>
/// 비즈니스 로직 에러 -> 클라이언트에 메시지 노출
/// Exception을 한번 래핑해서 Exception과 구분해서 시스템 오류랑 비지니스 오류를 따로 처리 가능. (GlobalExceptionMiddleware)
/// </summary>
public class BusinessException : Exception {
    public int StatusCode { get; }

    public BusinessException(string message, int statusCode = 400) : base(message) {
        StatusCode = statusCode;
    }
}

/// <summary>
/// 400 Bad Request - 잘못된 요청
/// </summary>
public class BadRequestException : BusinessException {
    public BadRequestException(string message) : base(message, 400) { }
}

/// <summary>
/// 401 Unauthorized - 인증 실패
/// </summary>
public class UnauthorizedException : BusinessException {
    public UnauthorizedException(string message) : base(message, 401) { }
}

/// <summary>
/// 403 Forbidden - 권한 없음
/// </summary>
public class ForbiddenException : BusinessException {
    public ForbiddenException(string message) : base(message, 403) { }
}

/// <summary>
/// 404 Not Found - 리소스 없음
/// </summary>
public class NotFoundException : BusinessException {
    public NotFoundException(string message) : base(message, 404) { }
}

/// <summary>
/// 409 Conflict - 충돌 (중복 등)
/// </summary>
public class ConflictException : BusinessException {
    public ConflictException(string message) : base(message, 409) { }
}

// TODO: 필요할때마다 추가