using System.Text.Json;
using System.Text.Json.Serialization;
using System.Net.Sockets;

namespace MessengerServer.Protocol;


public class ServerMessage
{
    [JsonPropertyName("action")]
    public string Action { get; set; } = string.Empty; 

    [JsonPropertyName("data")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public JsonElement? Data { get; set; }

    [JsonPropertyName("success")]
    public bool Success { get; set; } = true;

    [JsonPropertyName("error")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Error { get; set; }

    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.Now;

    [JsonPropertyName("request_id")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? RequestId { get; set; }


    [JsonPropertyName("broadcast")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? BroadcastTo { get; set; }
}


public static class MessageProtocolHelper
{

    public static async Task SendMessageWithPrefixAsync(
        System.Net.Sockets.NetworkStream stream,
        object messageData,
        SemaphoreSlim? streamLock = null)
    {
        try
        {
            if (streamLock != null)
                await streamLock.WaitAsync();

            try
            {
                string json = JsonSerializer.Serialize(messageData);
                byte[] messageBytes = System.Text.Encoding.UTF8.GetBytes(json);

                byte[] lengthBytes = BitConverter.GetBytes(messageBytes.Length);
                if (BitConverter.IsLittleEndian)
                    Array.Reverse(lengthBytes);

                await stream.WriteAsync(lengthBytes, 0, 4);
                await stream.WriteAsync(messageBytes, 0, messageBytes.Length);
                await stream.FlushAsync();
            }
            finally
            {
                streamLock?.Release();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error sending message: {ex.Message}");
            throw;
        }
    }


    public static async Task<T?> ReadMessageWithPrefixAsync<T>(
        System.Net.Sockets.NetworkStream stream,
        CancellationToken cancellationToken = default) where T : class
    {
        try
        {
            byte[] lengthBuffer = new byte[4];
            int lengthBytesRead = await stream.ReadAsync(lengthBuffer, 0, 4, cancellationToken);

            if (lengthBytesRead == 0)
            {
                Console.WriteLine("Connection closed by remote host");
                return null;
            }

            if (lengthBytesRead != 4)
            {
                Console.WriteLine($"❌ Invalid length prefix: expected 4 bytes, got {lengthBytesRead}");
                return null;
            }

            if (BitConverter.IsLittleEndian)
                Array.Reverse(lengthBuffer);

            int messageLength = BitConverter.ToInt32(lengthBuffer, 0);

            if (messageLength <= 0 || messageLength > 52428800) 
            {
                Console.WriteLine($"❌ Invalid message length: {messageLength}");
                return null;
            }

            byte[] messageBuffer = new byte[messageLength];
            int totalBytesRead = 0;

            while (totalBytesRead < messageLength)
            {
                int bytesRead = await stream.ReadAsync(
                    messageBuffer,
                    totalBytesRead,
                    messageLength - totalBytesRead,
                    cancellationToken);

                if (bytesRead == 0)
                {
                    Console.WriteLine("❌ Connection closed while reading message");
                    return null;
                }

                totalBytesRead += bytesRead;
            }

            string json = System.Text.Encoding.UTF8.GetString(messageBuffer);
            var message = JsonSerializer.Deserialize<T>(json);

            return message;
        }
        catch (OperationCanceledException)
        {
            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error reading message: {ex.Message}");
            return null;
        }
    }
}

// DTO
public class LoginRequest
{
    [JsonPropertyName("username")]
    public string Username { get; set; } = string.Empty;

    [JsonPropertyName("password")]
    public string Password { get; set; } = string.Empty;
}

public class RegisterRequest
{
    [JsonPropertyName("username")]
    public string Username { get; set; } = string.Empty;

    [JsonPropertyName("password")]
    public string Password { get; set; } = string.Empty;
}

public class SendMessageRequest
{
    [JsonPropertyName("chat_id")]
    public int ChatId { get; set; }

    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = "Text";
}

public class CreateGroupRequest
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string? Description { get; set; }
}

public class GetMessagesRequest
{
    [JsonPropertyName("chat_id")]
    public int ChatId { get; set; }

    [JsonPropertyName("limit")]
    public int Limit { get; set; } = 50;
}

public class SearchUsersRequest
{
    [JsonPropertyName("query")]
    public string Query { get; set; } = string.Empty;
}

// DTO для відповідей сервера
public class UserDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("username")]
    public string Username { get; set; } = string.Empty;

    [JsonPropertyName("bio")]
    public string? Bio { get; set; }

    [JsonPropertyName("birth_date")]
    public DateTime? BirthDate { get; set; }

    [JsonPropertyName("avatar_path")]
    public string? AvatarPath { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = "Offline";
}

public class MessageDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("chat_id")]
    public int ChatId { get; set; }

    [JsonPropertyName("sender_id")]
    public int SenderId { get; set; }

    [JsonPropertyName("sender_username")]
    public string SenderUsername { get; set; } = string.Empty;

    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;

    [JsonPropertyName("sent_at")]
    public DateTime SentAt { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = "Sent";
}

public class ChatDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("is_group")]
    public bool IsGroup { get; set; }

    [JsonPropertyName("last_message")]
    public string? LastMessage { get; set; }

    [JsonPropertyName("last_message_time")]
    public DateTime? LastMessageTime { get; set; }

    [JsonPropertyName("members")]
    public List<UserDto> Members { get; set; } = new();
}

public class GroupDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("chat_id")]
    public int ChatId { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("owner_id")]
    public int OwnerId { get; set; }

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("members")]
    public List<UserDto> Members { get; set; } = new();
}