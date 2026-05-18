using MessengerServer.Data;
using MessengerServer.Models;
using Microsoft.EntityFrameworkCore;

namespace MessengerServer.Services;

public class UserService
{
    private readonly DbService _dbService;

    public UserService(DbService dbService)
    {
        _dbService = dbService;
    }

    public async Task<User?> GetUserByIdAsync(int userId)
    {
        using (var context = _dbService.CreateContext())
        {
            return await context.Users.FirstOrDefaultAsync(u => u.Id == userId);
        }
    }

    public async Task<List<User>> SearchUsersAsync(string query)
    {
        using (var context = _dbService.CreateContext())
        {
            return await context.Users
                .Where(u => u.Username.Contains(query))
                .Take(20)
                .ToListAsync();
        }
    }

    public async Task<List<User>> GetUserContactsAsync(int userId)
    {
        using (var context = _dbService.CreateContext())
        {
            return await context.Contacts
                .Where(c => c.UserId == userId)
                .Include(c => c.ContactUser)
                .Select(c => c.ContactUser)
                .ToListAsync();
        }
    }

    public async Task<Contact> AddContactAsync(int userId, int contactUserId)
    {
        using (var context = _dbService.CreateContext())
        {
            var existing = await context.Contacts
                .FirstOrDefaultAsync(c => c.UserId == userId && c.ContactUserId == contactUserId);

            if (existing != null)
                return existing;

            var contact = new Contact
            {
                UserId = userId,
                ContactUserId = contactUserId
            };

            context.Contacts.Add(contact);
            await context.SaveChangesAsync();
            return contact;
        }
    }

    public async Task<bool> RemoveContactAsync(int userId, int contactUserId)
    {
        using (var context = _dbService.CreateContext())
        {
            var contact = await context.Contacts
                .FirstOrDefaultAsync(c => c.UserId == userId && c.ContactUserId == contactUserId);

            if (contact == null)
                return false;

            context.Contacts.Remove(contact);
            await context.SaveChangesAsync();
            return true;
        }
    }

    public async Task<User?> UpdateUserProfileAsync(int userId, string? bio = null, DateTime? birthDate = null, string? avatarPath = null)
    {
        using (var context = _dbService.CreateContext())
        {
            var user = await context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
                return null;

            if (!string.IsNullOrEmpty(bio))
                user.Bio = bio;
            if (birthDate.HasValue)
                user.BirthDate = birthDate;
            if (!string.IsNullOrEmpty(avatarPath))
                user.AvatarPath = avatarPath;

            await context.SaveChangesAsync();
            return user;
        }
    }

    public async Task SetUserStatusAsync(int userId, string status)
    {
        using (var context = _dbService.CreateContext())
        {
            var user = await context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user != null && Enum.TryParse<UserStatus>(status, out var userStatus))
            {
                user.Status = userStatus;
                await context.SaveChangesAsync();
            }
        }
    }
}
