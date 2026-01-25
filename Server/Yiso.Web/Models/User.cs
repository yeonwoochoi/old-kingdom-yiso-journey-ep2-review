namespace Yiso.Web.Models;

/// <summary>
/// 이게 Entity -> 지금은 임시 json 파일 나중엔 DB 테이블에 저장됨.
/// </summary>
public class User {
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty; // bcrypt로 해싱
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
