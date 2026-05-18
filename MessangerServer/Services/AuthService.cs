using BCrypt.Net;
using MessengerServer.Data;
using MessengerServer.Models;

namespace MessengerServer.Services;

public class AuthService
{
    private readonly DbService _dbService;

    public AuthService(DbService dbService)
    {
        _dbService = dbService;
    }

    public async Task<User?> LoginAsync(string username, string password)
    {
        var user = await _dbService.FindUserByUsernameAsync(username);
        if (user == null) return null;

        bool isValid = BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);
        return isValid ? user : null;
    }

    public async Task<User?> RegisterAsync(string username, string password)
    {

        bool userExists = await _dbService.UserExistsAsync(username);
        if (userExists)
            return null;

        string hashedPassword = BCrypt.Net.BCrypt.HashPassword(password);

        return await _dbService.CreateUserAsync(username, hashedPassword);
    }
}

