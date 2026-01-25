using Yiso.Web.Services.Interfaces;

namespace Yiso.Web.Services;

public class PasswordService : IPasswordService {
    private const int WorkFactor = 11; // 높을수록 안전하지만 느림. 기본값인 11로 설정

    public string HashPassword(string password) {
        return BCrypt.Net.BCrypt.HashPassword(password, WorkFactor);
    }

    public bool VerifyPassword(string password, string hash) {
        return BCrypt.Net.BCrypt.Verify(password, hash);
    }
}
