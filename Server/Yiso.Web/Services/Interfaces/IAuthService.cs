using Yiso.Web.DTOs;
using Yiso.Web.Models;

namespace Yiso.Web.Services.Interfaces;

public interface IAuthService {
    Task<AuthResponse> RegisterAsync(RegisterRequest request);
    Task<AuthResponse> LoginAsync(LoginRequest request);
    Task<User?> GetUserByIdAsync(string id);
}
