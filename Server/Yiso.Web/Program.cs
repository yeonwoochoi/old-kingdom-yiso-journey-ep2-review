using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using StackExchange.Redis;
using Yiso.Web.Data;
using Yiso.Web.Middlewares;
using Yiso.Web.Repositories;
using Yiso.Web.Repositories.Interfaces;
using Yiso.Web.Services;
using Yiso.Web.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// ===========================================
// 1. 서비스 등록 (DI 컨테이너)
// ===========================================

builder.Services.AddControllers();
builder.Services.AddOpenApi();

// ----- MySQL + EF Core -----
var mysqlConnectionString = builder.Configuration.GetConnectionString("MySQL")
    ?? throw new InvalidOperationException("MySQL 연결 문자열이 설정되지 않았습니다.");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(mysqlConnectionString, ServerVersion.AutoDetect(mysqlConnectionString)));

// ----- Redis -----
var redisConnectionString = builder.Configuration.GetConnectionString("Redis")
    ?? throw new InvalidOperationException("Redis 연결 문자열이 설정되지 않았습니다.");
builder.Services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(redisConnectionString));

// ----- Repositories -----
builder.Services.AddScoped<IUserRepository, UserRepository>();       // MySQL
builder.Services.AddSingleton<ISessionRepository, RedisSessionRepository>(); // Redis

// ----- Services -----
builder.Services.AddSingleton<IPasswordService, PasswordService>();
builder.Services.AddScoped<IAuthService, AuthService>();


// ===========================================
// 2. 앱 빌드 및 미들웨어 파이프라인 구성
// ===========================================

var app = builder.Build();

// 전역 예외 처리 미들웨어 (가장 먼저 등록)
app.UseGlobalExceptionHandler();

// 개발 환경에서 OpenAPI 활성화 (Swagger)
if (app.Environment.IsDevelopment()) {
    app.MapOpenApi();
    app.MapScalarApiReference(); // Scalar UI: /scalar/v1
}

app.MapControllers();

app.Run();
