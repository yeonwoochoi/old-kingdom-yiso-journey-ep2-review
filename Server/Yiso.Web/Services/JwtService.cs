using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Yiso.Web.Models;
using Yiso.Web.Services.Interfaces;

namespace Yiso.Web.Services;

public class JwtService : IJwtService {
    private readonly IConfiguration _configuration;
    
    private readonly int _expiryMinutes; // 기본값 = 60분
    private readonly string _secretKey; // JWT 서명애 사용.
    private readonly string _issuer; // 토큰 발급자
    private readonly string _audience; // 토큰 수신자

    public JwtService(IConfiguration configuration) {
        _configuration = configuration;

        // appsettings.json에서 JWT 설정 읽기
        _secretKey = _configuration["Jwt:SecretKey"]
                     ?? throw new InvalidOperationException("JWT SecretKey가 설정되지 않았습니다.");
        _issuer = _configuration["Jwt:Issuer"] ?? "YisoServer";
        _audience = _configuration["Jwt:Audience"] ?? "YisoClient";
        _expiryMinutes = int.Parse(_configuration["Jwt:ExpiryMinutes"] ?? "60");
    }
    
    public string GenerateToken(User user) {
        // 대칭키 생성
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey));

        // 서명 자격 증명
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        // 토큰에 포함할 클레임들
        var claims = new[] {
            // Subject: 사용자 고유 ID
            // 보통 DB에서 PK값 많이 넣음
            new Claim(JwtRegisteredClaimNames.Sub, user.Id),
            new Claim(JwtRegisteredClaimNames.Name, user.Username),
            // Jti = JWT ID (토큰 고유 ID)
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            // Iat = Issued At (토큰 발급 시간)
            new Claim(JwtRegisteredClaimNames.Iat,
                DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(),
                ClaimValueTypes.Integer64)
        };

        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audience,
            claims: claims,
            expires: GetExpirationTime(),
            signingCredentials: credentials
        );

        // 토큰을 문자열로 직렬화
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
    
    public DateTime GetExpirationTime() {
        return DateTime.UtcNow.AddMinutes(_expiryMinutes);
    }
}
