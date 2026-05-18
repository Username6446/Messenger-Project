namespace MessengerServer.Models;

public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string? Bio { get; set; }
    public DateTime? BirthDate { get; set; }
    public string? AvatarPath { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public UserStatus Status { get; set; } = UserStatus.Offline;
    public ICollection<Message> SentMessages { get; set; } = new List<Message>();
    public ICollection<ChatMember> ChatMemberships { get; set; } = new List<ChatMember>();
    public ICollection<Contact> Contacts { get; set; } = new List<Contact>();
    public ICollection<Contact> ContactedBy { get; set; } = new List<Contact>();
    public ICollection<Group> OwnedGroups { get; set; } = new List<Group>();
}

public enum UserStatus
{
    Online,
    Offline,
    Away
}
