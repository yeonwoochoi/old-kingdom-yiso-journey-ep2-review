using System.Text.Json;
using Yiso.Web.Models;
using Yiso.Web.Repositories.Interfaces;

namespace Yiso.Web.Repositories;

public class FileUserRepository : IUserRepository {
    private readonly string _filePath;

    private readonly object _lock = new();

    private readonly JsonSerializerOptions _jsonOptions = new() {
        WriteIndented = true
    };

    public FileUserRepository() {
        var dataDir = Path.Combine(AppContext.BaseDirectory, "Data");

        if (!Directory.Exists(dataDir)) {
            Directory.CreateDirectory(dataDir);
        }

        _filePath = Path.Combine(dataDir, "users.json");

        if (!File.Exists(_filePath)) {
            File.WriteAllText(_filePath, "[]");
        }
    }

    public Task<User?> GetByUsernameAsync(string username) {
        var users = LoadUsers();
        var user = users.FirstOrDefault(u =>
            u.Username.Equals(username, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(user);
    }

    public Task<User?> GetByIdAsync(string id) {
        var users = LoadUsers();
        var user = users.FirstOrDefault(u => u.Id == id);
        return Task.FromResult(user);
    }

    public Task<User> CreateAsync(User user) {
        var users = LoadUsers();
        users.Add(user);
        SaveUsers(users);
        return Task.FromResult(user);
    }

    public Task<bool> ExistsAsync(string username) {
        var users = LoadUsers();
        var exists = users.Any(u =>
            u.Username.Equals(username, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(exists);
    }
    
    /// <summary>
    /// 일단 동기 블로킹 반식으로 구현
    /// lock은 await 사용 불가 (세마포어 쓰면 비동기로 처리 가능하지만 DB 연동할때 바꿔도 충분)
    /// </summary>
    /// <returns></returns>
    private List<User> LoadUsers() {
        lock (_lock) {
            var json = File.ReadAllText(_filePath);
            return JsonSerializer.Deserialize<List<User>>(json) ?? new List<User>();
        }
    }
    
    private void SaveUsers(List<User> users) {
        lock (_lock) {
            var json = JsonSerializer.Serialize(users, _jsonOptions);
            File.WriteAllText(_filePath, json);
        }
    }
}
