namespace MessengerServer.Models;
public class Message
{
    public int Id { get; set; }
    public int ChatId { get; set; }
    public int SenderId { get; set; }
    public string Text { get; set; } = string.Empty;
    public DateTime SentAt { get; set; } = DateTime.UtcNow;
    public MessageStatus Status { get; set; } = MessageStatus.Sent;
    public string Type { get; set; } = "Text"; 
    public Chat Chat { get; set; } = null!;
    public User Sender { get; set; } = null!;
}
public enum MessageStatus
{
    Sent,
    Delivered,
    Read
}
