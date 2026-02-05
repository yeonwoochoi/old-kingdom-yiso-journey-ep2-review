using System;

namespace Yiso.Shared.DTOs.Auth;

public class AuthResponse {
    public string SessionId { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
}
