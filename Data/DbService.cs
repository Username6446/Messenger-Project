using Microsoft.EntityFrameworkCore;
using Messenger_Project.Models;

namespace Messenger_Project.Data;

public class DbService
{
    private readonly AppDbContextFactory _factory;

    public DbService()
    {
        _factory = new AppDbContextFactory();
    }

    public AppDbContext CreateContext()
    {
        return _factory.CreateDbContext(Array.Empty<string>());
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
                CreatedAt = DateTime.UtcNow
            };

            context.Users.Add(user);
            await context.SaveChangesAsync();
            return user;
        }
    }

    public async Task<Message> SaveMessageAsync(int chatId, int senderId, string text, string type = "Text")
    {
        using (var context = CreateContext())
        {
            var message = new Message
            {
                ChatId = chatId,
                SenderId = senderId,
                Text = text,
                SentAt = DateTime.UtcNow,
                IsRead = false,
                Type = type
            };

            context.Messages.Add(message);
            await context.SaveChangesAsync();
            return message;
        }
    }

    public async Task<List<Message>> GetChatMessagesAsync(int chatId)
    {
        using (var context = CreateContext())
        {
            return await context.Messages
                .Where(m => m.ChatId == chatId)
                .Include(m => m.Sender)
                .OrderBy(m => m.SentAt)
                .ToListAsync();
        }
    }

    public async Task<List<Chat>> GetUserChatsAsync(int userId)
    {
        using (var context = CreateContext())
        {
            return await context.ChatMembers
                .Where(cm => cm.UserId == userId)
                .Include(cm => cm.Chat)
                .ThenInclude(c => c.Messages)
                .Select(cm => cm.Chat)
                .ToListAsync();
        }
    }

    public async Task<Chat> CreateDirectChatAsync(int userId1, int userId2)
    {
        using (var context = CreateContext())
        {
            var chat = new Chat
            {
                CreatedAt = DateTime.UtcNow,
                IsGroup = false
            };

            context.Chats.Add(chat);
            await context.SaveChangesAsync();

            var member1 = new ChatMember
            {
                ChatId = chat.Id,
                UserId = userId1,
                JoinedAt = DateTime.UtcNow,
                Role = "Member"
            };

            var member2 = new ChatMember
            {
                ChatId = chat.Id,
                UserId = userId2,
                JoinedAt = DateTime.UtcNow,
                Role = "Member"
            };

            context.ChatMembers.Add(member1);
            context.ChatMembers.Add(member2);
            await context.SaveChangesAsync();

            return chat;
        }
    }

    public async Task<Group> CreateGroupAsync(string name, int ownerId, string? description = null)
    {
        using (var context = CreateContext())
        {
            var chat = new Chat
            {
                CreatedAt = DateTime.UtcNow,
                IsGroup = true
            };

            context.Chats.Add(chat);
            await context.SaveChangesAsync();

            var group = new Group
            {
                ChatId = chat.Id,
                Name = name,
                Description = description,
                OwnerId = ownerId,
                CreatedAt = DateTime.UtcNow
            };

            context.Groups.Add(group);

            var ownerMember = new ChatMember
            {
                ChatId = chat.Id,
                UserId = ownerId,
                JoinedAt = DateTime.UtcNow,
                Role = "Owner"
            };

            context.ChatMembers.Add(ownerMember);
            await context.SaveChangesAsync();

            return group;
        }
    }

    public async Task<Contact> AddContactAsync(int userId, int contactUserId)
    {
        using (var context = CreateContext())
        {
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

    public async Task<List<User>> GetUserContactsAsync(int userId)
    {
        using (var context = CreateContext())
        {
            return await context.Contacts
                .Where(c => c.UserId == userId)
                .Include(c => c.ContactUser)
                .Select(c => c.ContactUser)
                .ToListAsync();
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

    public async Task<User?> UpdateUserProfileAsync(int userId, string? bio = null, DateTime? birthDate = null, string? avatarPath = null)
    {
        using (var context = CreateContext())
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
}
