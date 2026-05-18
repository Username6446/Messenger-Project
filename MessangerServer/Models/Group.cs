namespace MessengerServer.Models;

public class Group
{
    public int Id { get; set; }
    public int ChatId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? AvatarPath { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int OwnerId { get; set; }

    public Chat Chat { get; set; } = null!;
    public User Owner { get; set; } = null!;
}
