using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Yiso.Web.DTOs;
using Yiso.Web.Services.Interfaces;

namespace Yiso.Web.Controllers;

/// <summary>
/// [ApiController] = 인증 관련 API 컨트롤러라는 뜻 (html같은거 안주고 데이터 JSON 주고받는 API란 소리)
/// [Route("api/[controller]")] = AuthController란 클래스명에서 Controller만 뺀 Auth가 저 api 주소로 들어감
/// ControllerBase = API 기능만 딱 들어있어서 가벼움 (예전엔 Controller 상속 받았는데 HTML View 기능까지 있어서 무거움)
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
    /// 현재 로그인한 사용자 정보 조회 API
    /// GET /api/auth/me
    /// Authorization: Bearer {token} 헤더 필요
    /// </summary>
    [HttpGet("me")]
    [Authorize] // JWT 인증 필요 (program.cs에서 설정해둠)
    public async Task<ActionResult> GetCurrentUser() {
        // JWT 토큰에서 사용자 ID 추출 (Sub 클레임)
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(userId)) {
            return Unauthorized(new { message = "유효하지 않은 토큰입니다." });
        }

        var user = await _authService.GetUserByIdAsync(userId);

        if (user == null) {
            return NotFound(new { message = "사용자를 찾을 수 없습니다." });
        }

        return Ok(new {
            id = user.Id,
            username = user.Username,
            createdAt = user.CreatedAt
        });
    }
}
