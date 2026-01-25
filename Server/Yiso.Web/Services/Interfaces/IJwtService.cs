using System.Security.Claims;
using Yiso.Web.Models;

namespace Yiso.Web.Services.Interfaces;

public interface IJwtService {
    string GenerateToken(User user);
    DateTime GetExpirationTime();
}
