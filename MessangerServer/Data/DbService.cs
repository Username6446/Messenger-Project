using MessengerServer.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration.Json;
using System.IO;
namespace MessengerServer.Data;

public class DbService
{
    private readonly string _connectionString;

    public DbService()
    {
        var basePath = AppContext.BaseDirectory;
        var configPath = Path.Combine(basePath, "appsettings.json");

        Console.WriteLine($"📁 Looking for config at: {configPath}");

        var configuration = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();

        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "Connection string 'DefaultConnection' not found in appsettings.json\n" +
                $"Expected file at: {configPath}");

        Console.WriteLine("✅ Connection string loaded successfully");
        Console.WriteLine($"   Database: MessengerDB");
        Console.WriteLine($"   Server: 192.168.0.113\n");
    }

    public AppDbContext CreateContext()
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseSqlServer(_connectionString);
        return new AppDbContext(optionsBuilder.Options);
    }

    public async Task<User?> FindUserByUsernameAsync(string username)
    {
        using (var context = CreateContext())
        {
            return await context.Users
                .FirstOrDefaultAsync(u => u.Username == username);
        }
    }

    public async Task<User?> FindUserByIdAsync(int userId)
    {
        using (var context = CreateContext())
        {
            return await context.Users
                .Include(u => u.SentMessages)
                .Include(u => u.ChatMemberships)
                .Include(u => u.Contacts)
                .Include(u => u.ContactedBy)
                .Include(u => u.OwnedGroups)
                .FirstOrDefaultAsync(u => u.Id == userId);
        }
    }

    public async Task<User> CreateUserAsync(string username, string passwordHash, string? bio = null, DateTime? birthDate = null)
    {
        using (var context = CreateContext())
        {
            var user = new User
            {
                Username = username,
                PasswordHash = passwordHash,
                Bio = bio,
                BirthDate = birthDate,
                CreatedAt = DateTime.UtcNow,
                Status = UserStatus.Offline
            };

            context.Users.Add(user);
            await context.SaveChangesAsync();
            return user;
        }
    }

    public async Task<bool> UserExistsAsync(string username)
    {
        using (var context = CreateContext())
        {
            return await context.Users
                .AnyAsync(u => u.Username == username);
        }
    }

    public async Task InitializeDatabaseAsync()
    {
        try
        {
            using (var context = CreateContext())
            {
                Console.WriteLine("🔧 Applying database migrations...");
                await context.Database.MigrateAsync();
                Console.WriteLine("✅ Database migrations applied successfully!");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Database initialization error: {ex.Message}");
            Console.WriteLine($"   Make sure SQL Server is running and connection string is correct");
            throw;
        }
    }
}