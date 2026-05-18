namespace MessengerServer.Models;

public class Chat
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsGroup { get; set; } = false;

    public Group? Group { get; set; }
    public ICollection<Message> Messages { get; set; } = new List<Message>();
    public ICollection<ChatMember> Members { get; set; } = new List<ChatMember>();
}
