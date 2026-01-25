using Yiso.Web.Models;

namespace Yiso.Web.Repositories.Interfaces;

public interface IUserRepository {
    Task<User?> GetByUsernameAsync(string username);
    Task<User?> GetByIdAsync(string id);
    Task<User> CreateAsync(User user);
    Task<bool> ExistsAsync(string username);
}
