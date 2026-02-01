using Microsoft.AspNetCore.Mvc;
using Yiso.Web.DTOs;
using Yiso.Web.Filters;
using Yiso.Web.Models;
using Yiso.Web.Services.Interfaces;

namespace Yiso.Web.Controllers;

/// <summary>
/// 인증 관련 API 컨트롤러 (세션 기반)
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase {
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService) {
        _authService = authService;
    }

    /// <summary>
    /// 회원가입 API
    /// POST /api/auth/register
    /// </summary>
    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest request) {
        try {
            var response = await _authService.RegisterAsync(request);
            return Ok(response);
        }
        catch (InvalidOperationException ex) {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// 로그인 API
    /// POST /api/auth/login
    /// </summary>
    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request) {
        try {
            var response = await _authService.LoginAsync(request);
            return Ok(response);
        }
        catch (UnauthorizedAccessException ex) {
            return Unauthorized(new { message = ex.Message });
        }
    }

    /// <summary>
    /// 로그아웃 API
    /// POST /api/auth/logout
    /// Header: X-Session-Id 필요
    /// </summary>
    [HttpPost("logout")]
    [SessionAuth]
    public async Task<ActionResult> Logout() {
        var sessionId = HttpContext.Items[SessionAuthAttribute.SessionIdHeader] as string;
        if (string.IsNullOrEmpty(sessionId)) {
            return BadRequest(new { message = "세션 ID를 찾을 수 없습니다." });
        }

        await _authService.LogoutAsync(sessionId);
        return Ok(new { message = "로그아웃 되었습니다." });
    }

    /// <summary>
    /// 현재 로그인한 사용자 정보 조회 API
    /// GET /api/auth/me
    /// Header: X-Session-Id 필요
    /// </summary>
    [HttpGet("me")]
    [SessionAuth]
    public ActionResult GetCurrentUser() {
        var sessionData = HttpContext.Items[SessionAuthAttribute.SessionDataKey] as SessionData;
        if (sessionData == null) {
            return Unauthorized(new { message = "세션 데이터를 찾을 수 없습니다." });
        }

        return Ok(new {
            userId = sessionData.UserId,
            username = sessionData.Username,
            createdAt = sessionData.CreatedAt,
            lastAccessedAt = sessionData.LastAccessedAt
        });
    }
}
