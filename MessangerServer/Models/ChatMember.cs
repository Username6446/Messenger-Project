namespace MessengerServer.Models;

public class ChatMember
{
    public int ChatId { get; set; }
    public int UserId { get; set; }
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    public string Role { get; set; } = "Member";

    public Chat Chat { get; set; } = null!;
    public User User { get; set; } = null!;
}
