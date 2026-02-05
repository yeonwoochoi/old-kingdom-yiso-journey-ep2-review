using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Yiso.Shared.DTOs.Common;
using Yiso.Web.Services.Interfaces;

namespace Yiso.Web.Filters;

/// <summary>
/// 세션 인증 필터
/// 요청 헤더의 X-Session-Id를 검증하고 HttpContext.Items에 세션 데이터 저장
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class SessionAuthAttribute : Attribute, IAsyncAuthorizationFilter {
    public const string SessionIdHeader = "X-Session-Id";
    public const string SessionDataKey = "SessionData";

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context) {
        // 헤더에서 세션 ID 추출
        if (!context.HttpContext.Request.Headers.TryGetValue(SessionIdHeader, out var sessionIdValues)) {
            context.Result = new UnauthorizedObjectResult(ErrorResponse.Create("세션 ID가 필요합니다.", StatusCodes.Status401Unauthorized));
            return;
        }

        var sessionId = sessionIdValues.FirstOrDefault();
        if (string.IsNullOrEmpty(sessionId)) {
            context.Result = new UnauthorizedObjectResult(ErrorResponse.Create("세션 ID가 필요합니다.", StatusCodes.Status401Unauthorized));
            return;
        }

        // DI에서 AuthService 가져오기
        var authService = context.HttpContext.RequestServices.GetRequiredService<IAuthService>();

        // 세션 검증
        var sessionData = await authService.ValidateSessionAsync(sessionId);
        if (sessionData == null) {
            context.Result = new UnauthorizedObjectResult(ErrorResponse.Create("유효하지 않거나 만료된 세션입니다.", StatusCodes.Status401Unauthorized));
            return;
        }

        // 세션 갱신 (슬라이딩 만료)
        await authService.RefreshSessionAsync(sessionId);

        // HttpContext에 세션 데이터 저장 (컨트롤러에서 사용)
        context.HttpContext.Items[SessionDataKey] = sessionData;
        context.HttpContext.Items[SessionIdHeader] = sessionId;
    }
}
