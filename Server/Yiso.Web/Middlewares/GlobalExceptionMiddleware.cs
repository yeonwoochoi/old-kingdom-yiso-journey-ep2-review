using System.Text.Json;
using Yiso.Web.DTOs;
using Yiso.Web.Exceptions;

namespace Yiso.Web.Middlewares;

/// <summary>
/// 전역 예외 처리 미들웨어
/// - BusinessException: 클라이언트에 메시지 노출
/// - 그 외 예외: 개발 환경에서만 상세 메시지 노출, 프로덕션에서는 일반 메시지
/// </summary>
public class GlobalExceptionMiddleware {
    private readonly RequestDelegate _next;
    private readonly IHostEnvironment _env;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(RequestDelegate next, IHostEnvironment env, ILogger<GlobalExceptionMiddleware> logger) {
        _next = next;
        _env = env;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context) {
        try {
            await _next(context);
        }
        // 비지니스 오류
        catch (BusinessException ex) {
            _logger.LogWarning(ex, "비즈니스 오류 발생: {Message}", ex.Message);
            await WriteErrorResponseAsync(context, ex.StatusCode, ex.Message);
        }
        // 시스템 오류
        catch (Exception ex) {
            _logger.LogError(ex, "시스템 오류 발생");
            var message = _env.IsDevelopment()
                ? ex.Message
                : "서버 오류가 발생했습니다. 잠시 후 다시 시도해주세요.";
            await WriteErrorResponseAsync(context, StatusCodes.Status500InternalServerError, message);
        }
    }

    private static async Task WriteErrorResponseAsync(HttpContext context, int statusCode, string message) {
        context.Response.ContentType = "application/json; charset=utf-8";
        context.Response.StatusCode = statusCode;

        var response = ErrorResponse.Create(message, statusCode);
        var json = JsonSerializer.Serialize(response, new JsonSerializerOptions {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(json);
    }
}

public static class GlobalExceptionMiddlewareExtensions {
    public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder app) {
        return app.UseMiddleware<GlobalExceptionMiddleware>();
    }
}
