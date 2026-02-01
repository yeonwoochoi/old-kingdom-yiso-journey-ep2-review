namespace Yiso.Web.Models;

/// <summary>
/// User Entity - MySQL users 테이블에 매핑
/// </summary>
public class User {
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
