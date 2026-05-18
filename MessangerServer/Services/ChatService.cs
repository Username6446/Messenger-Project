using MessengerServer.Data;
using MessengerServer.Models;
using Microsoft.EntityFrameworkCore;

namespace MessengerServer.Services;

public class ChatService
{
    private readonly DbService _dbService;

    public ChatService(DbService dbService)
    {
        _dbService = dbService;
    }

    public async Task<Message?> SendMessageAsync(int chatId, int senderId, string text, string type = "Text")
    {
        using (var context = _dbService.CreateContext())
        {
            bool isInChat = await context.ChatMembers
                .AnyAsync(cm => cm.ChatId == chatId && cm.UserId == senderId);

            if (!isInChat)
                return null;

            var message = new Message
            {
                ChatId = chatId,
                SenderId = senderId,
                Text = text,
                SentAt = DateTime.UtcNow,
                Type = type,
                Status = MessageStatus.Sent
            };

            context.Messages.Add(message);
            await context.SaveChangesAsync();


            await context.Entry(message).Reference(m => m.Sender).LoadAsync();
            return message;
        }
    }

    public async Task<List<Message>> GetChatMessagesAsync(int chatId, int limit = 50)
    {
        using (var context = _dbService.CreateContext())
        {
            return await context.Messages
                .Where(m => m.ChatId == chatId)
                .Include(m => m.Sender)
                .OrderByDescending(m => m.SentAt)
                .Take(limit)
                .OrderBy(m => m.SentAt)
                .ToListAsync();
        }
    }

    public async Task MarkMessageAsReadAsync(int messageId)
    {
        using (var context = _dbService.CreateContext())
        {
            var message = await context.Messages.FindAsync(messageId);
            if (message != null)
            {
                message.Status = MessageStatus.Read;
                await context.SaveChangesAsync();
            }
        }
    }

    public async Task<Chat?> GetOrCreateDirectChatAsync(int userId1, int userId2)
    {
        using (var context = _dbService.CreateContext())
        {
  
            var existingChat = await context.Chats
                .Include(c => c.Members)
                .Where(c => c.IsGroup == false)
                .FirstOrDefaultAsync(c =>
                    c.Members.Count == 2 &&
                    c.Members.Any(m => m.UserId == userId1) &&
                    c.Members.Any(m => m.UserId == userId2));

            if (existingChat != null)
                return existingChat;
            var chat = new Chat { IsGroup = false, CreatedAt = DateTime.UtcNow };
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

    public async Task<List<Chat>> GetUserChatsAsync(int userId)
    {
        using (var context = _dbService.CreateContext())
        {
            return await context.ChatMembers
                .Where(cm => cm.UserId == userId)
                .Include(cm => cm.Chat)
                    .ThenInclude(c => c.Messages)
                    .OrderByDescending(c => c.Chat.Messages.Max(m => m.SentAt))
                .Include(cm => cm.Chat)
                    .ThenInclude(c => c.Group)
                .Include(cm => cm.Chat)
                    .ThenInclude(c => c.Members)
                    .ThenInclude(m => m.User)
                .Select(cm => cm.Chat)
                .ToListAsync();
        }
    }

    public async Task<bool> IsUserInChatAsync(int userId, int chatId)
    {
        using (var context = _dbService.CreateContext())
        {
            return await context.ChatMembers
                .AnyAsync(cm => cm.UserId == userId && cm.ChatId == chatId);
        }
    }

    public async Task<List<int>> GetChatMemberIdsAsync(int chatId)
    {
        using (var context = _dbService.CreateContext())
        {
            return await context.ChatMembers
                .Where(cm => cm.ChatId == chatId)
                .Select(cm => cm.UserId)
                .ToListAsync();
        }
    }

    public async Task<Chat?> GetChatByIdAsync(int chatId)
    {
        using (var context = _dbService.CreateContext())
        {
            return await context.Chats
                .Include(c => c.Group)
                .Include(c => c.Members)
                    .ThenInclude(m => m.User)
                .Include(c => c.Messages)
                .FirstOrDefaultAsync(c => c.Id == chatId);
        }
    }


    public async Task<Group?> CreateGroupAsync(string name, int ownerId, string? description = null)
    {
        using (var context = _dbService.CreateContext())
        {
            var chat = new Chat { IsGroup = true, CreatedAt = DateTime.UtcNow };
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
                Role = "Owner",
                JoinedAt = DateTime.UtcNow
            };

            context.ChatMembers.Add(ownerMember);
            await context.SaveChangesAsync();

            return group;
        }
    }

    public async Task<bool> AddUserToGroupAsync(int groupId, int userId, string role = "Member")
    {
        using (var context = _dbService.CreateContext())
        {
            var group = await context.Groups.FirstOrDefaultAsync(g => g.Id == groupId);
            if (group == null)
                return false;

            bool alreadyMember = await context.ChatMembers
                .AnyAsync(cm => cm.ChatId == group.ChatId && cm.UserId == userId);

            if (alreadyMember)
                return false;

            var member = new ChatMember
            {
                ChatId = group.ChatId,
                UserId = userId,
                Role = role,
                JoinedAt = DateTime.UtcNow
            };

            context.ChatMembers.Add(member);
            await context.SaveChangesAsync();
            return true;
        }
    }

    public async Task<bool> RemoveUserFromGroupAsync(int groupId, int userId)
    {
        using (var context = _dbService.CreateContext())
        {
            var group = await context.Groups.FirstOrDefaultAsync(g => g.Id == groupId);
            if (group == null)
                return false;

            var member = await context.ChatMembers
                .FirstOrDefaultAsync(cm => cm.ChatId == group.ChatId && cm.UserId == userId);

            if (member == null)
                return false;

            context.ChatMembers.Remove(member);
            await context.SaveChangesAsync();
            return true;
        }
    }
}