using Microsoft.EntityFrameworkCore;
using Yiso.Web.Data;
using Yiso.Web.Models;
using Yiso.Web.Repositories.Interfaces;

namespace Yiso.Web.Repositories;

/// <summary>
/// EF Core + MySQL 기반 UserRepository
/// </summary>
public class UserRepository : IUserRepository {
    private readonly AppDbContext _context;

    public UserRepository(AppDbContext context) {
        _context = context;
    }

    public async Task<User?> GetByUsernameAsync(string username) {
        return await _context.Users.FirstOrDefaultAsync(u => u.Username.ToLower() == username.ToLower());
    }

    public async Task<User?> GetByIdAsync(string id) {
        return await _context.Users.FindAsync(id);
    }

    public async Task<User> CreateAsync(User user) {
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        return user;
    }

    public async Task<bool> ExistsAsync(string username) {
        return await _context.Users.AnyAsync(u => u.Username.ToLower() == username.ToLower());
    }
}
