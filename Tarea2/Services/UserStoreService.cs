using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Tarea2.Models;

namespace Tarea2.Services;

public interface IUserStoreService
{
    IReadOnlyList<UserRecord> GetAll();
    bool UsernameExists(string username);
    void AddUser(UserRecord user);
}

public class JsonUserStoreService : IUserStoreService
{
    private readonly string _dataPath;
    private readonly JsonSerializerOptions _options = new() { WriteIndented = true };
    private readonly object _lock = new();

    public JsonUserStoreService(IHostEnvironment environment)
    {
        _dataPath = Path.Combine(environment.ContentRootPath, "Data", "users.json");
        if (!File.Exists(_dataPath))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_dataPath)!);
            File.WriteAllText(_dataPath, "[]");
        }
    }

    public IReadOnlyList<UserRecord> GetAll()
    {
        lock (_lock)
        {
            using var stream = File.OpenRead(_dataPath);
            var users = JsonSerializer.Deserialize<List<UserRecord>>(stream) ?? new List<UserRecord>();
            return users;
        }
    }

    public bool UsernameExists(string username)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            return false;
        }

        var normalized = username.Trim().ToLowerInvariant();
        return GetAll().Any(u => string.Equals(u.Username?.Trim().ToLowerInvariant(), normalized, StringComparison.Ordinal));
    }

    public void AddUser(UserRecord user)
    {
        lock (_lock)
        {
            List<UserRecord> users;
            using (var stream = File.OpenRead(_dataPath))
            {
                users = JsonSerializer.Deserialize<List<UserRecord>>(stream) ?? new List<UserRecord>();
            }

            if (users.Any(u => string.Equals(u.Username?.Trim(), user.Username.Trim(), StringComparison.OrdinalIgnoreCase)))
            {
                throw new InvalidOperationException("El usuario ya existe.");
            }

            users.Add(user);
            var json = JsonSerializer.Serialize(users, _options);
            File.WriteAllText(_dataPath, json);
        }
    }
}
