using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using Yiso.Web.Repositories;
using Yiso.Web.Repositories.Interfaces;
using Yiso.Web.Services;
using Yiso.Web.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);


// 1. 서비스 등록 (DI 컨테이너)
builder.Services.AddControllers(); // 컨트롤러 추가
builder.Services.AddOpenApi(); // OpenAPI (Swagger) 설정
builder.Services.AddSingleton<IUserRepository, FileUserRepository>();
builder.Services.AddSingleton<IPasswordService, PasswordService>();
builder.Services.AddSingleton<IJwtService, JwtService>();
builder.Services.AddScoped<IAuthService, AuthService>(); // 나중에 상태 저장할 수도 있어서 + 나중에 DB연동시 DBContext 때문 (얘가 Scoped임)


// 2. JWT 인증 설정
// Service.Add... => DI 컨테이너(서비스 목록)에 권한 검사할때 필요한 로직(클래스) 등록 과정임.

// appsettings.json에서 JWT 설정 읽기
var jwtSecretKey = builder.Configuration["Jwt:SecretKey"]
                   ?? throw new InvalidOperationException("JWT SecretKey가 설정되지 않았습니다.");
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "YisoServer";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "YisoClient";

// JWT 인증 스키마 추가
builder.Services.AddAuthentication(options => {
        // 기본 인증 스키마를 JWT Bearer로 설정
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options => {
        // 토큰 검증 매개변수 설정
        options.TokenValidationParameters = new TokenValidationParameters {
            // 발급자 검증
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,

            // 수신자 검증
            ValidateAudience = true,
            ValidAudience = jwtAudience,

            // 서명 키 검증
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecretKey)),

            // 토큰 만료 시간 검증
            ValidateLifetime = true,

            // 시간 오차 허용 범위 (서버 간 시간 차이 대응)
            // 원래는 서버간 시간차이 고려해서 어느정도 봐주는데
            // 보안 위해 걍 1초라도 지나면 만료시키게 설정 
            ClockSkew = TimeSpan.Zero
        };
    });

// 권한 부여 서비스 추가
builder.Services.AddAuthorization();


// 3. 앱 빌드 및 미들웨어 파이프라인 구성
// app.Use... => 미들웨어 등록 (모든 요청은 이 Middleware 거치게 되는거임. 하지만 필터 없으면 pass)
// ex. AuthController의 GetCurrentUser 함수보면 [Authorize] 필터가 있음 -> Authorization 미들웨어 사용한다는 소리
var app = builder.Build();

// 개발 환경에서 OpenAPI 활성화 (Swagger)
if (app.Environment.IsDevelopment()) {
    app.MapOpenApi();
    app.MapScalarApiReference(); // Scalar UI: /scalar/v1
}
app.UseAuthentication();
app.UseAuthorization(); // 권한 부여 미들웨어 ([Authorize] 어트리뷰트 처리)
app.MapControllers(); // 컨트롤러 엔드포인트 매핑

app.Run();
