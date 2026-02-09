using Microsoft.AspNetCore.Mvc;
using ServerShared.DTOs.Rank;
using ServerShared.Models;
using Yiso.Web.Filters;
using Yiso.Web.Services.Interfaces;

namespace Yiso.Web.Controllers;

/// <summary>
/// 랭킹 관련 API 컨트롤러
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class RankController : ControllerBase {
    private readonly IRankService _rankService;

    public RankController(IRankService rankService) {
        _rankService = rankService;
    }

    /// <summary>
    /// 점수 등록 API
    /// POST /api/rank/score
    /// Header: X-Session-Id 필요
    /// </summary>
    [HttpPost("score")]
    [SessionAuth]
    public async Task<ActionResult> RegisterScore([FromBody] RankRegisterRequest request) {
        var sessionData = HttpContext.Items[SessionAuthAttribute.SessionDataKey] as SessionData;
        await _rankService.RegisterScoreAsync(sessionData!.UserId, sessionData.Username, request);
        return Ok(new { message = "점수가 등록되었습니다." });
    }

    /// <summary>
    /// Top N 랭킹 조회 API
    /// GET /api/rank/top?count=10
    /// </summary>
    [HttpGet("top")]
    public async Task<ActionResult<RankListResponse>> GetTopRanks([FromQuery] int count = 10) {
        var response = await _rankService.GetTopRanksAsync(count);
        return Ok(response);
    }

    /// <summary>
    /// 내 랭킹 조회 API
    /// GET /api/rank/me
    /// Header: X-Session-Id 필요
    /// </summary>
    [HttpGet("me")]
    [SessionAuth]
    public async Task<ActionResult<RankResponse>> GetMyRank() {
        var sessionData = HttpContext.Items[SessionAuthAttribute.SessionDataKey] as SessionData;
        var response = await _rankService.GetMyRankAsync(sessionData!.UserId);
        if (response == null) {
            return NotFound(new { message = "등록된 랭킹이 없습니다." });
        }
        return Ok(response);
    }

    /// <summary>
    /// 내 랭킹 삭제 API
    /// DELETE /api/rank/me
    /// Header: X-Session-Id 필요
    /// </summary>
    [HttpDelete("me")]
    [SessionAuth]
    public async Task<ActionResult> DeleteMyRank() {
        var sessionData = HttpContext.Items[SessionAuthAttribute.SessionDataKey] as SessionData;
        await _rankService.DeleteRankAsync(sessionData!.UserId);
        return Ok(new { message = "랭킹이 삭제되었습니다." });
    }
}
