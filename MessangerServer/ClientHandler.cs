using MessengerServer.Protocol;
using MessengerServer.Services;
using System;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace MessengerServer;

public class ClientHandler
{
    private readonly TcpClient _client;
    private readonly NetworkStream _stream;
    private readonly AuthService _authService;
    private readonly ChatService _chatService;
    private readonly UserService _userService;
    private readonly Server _server;

    public int CurrentUserId { get; private set; } = 0;
    public string ClientEndPoint { get; }

    private bool _isConnected = true;

    public ClientHandler(
        TcpClient client,
        AuthService authService,
        ChatService chatService,
        UserService userService,
        Server server)
    {
        _client = client;
        _stream = client.GetStream();
        _authService = authService;
        _chatService = chatService;
        _userService = userService;
        _server = server;
        ClientEndPoint = client.Client.RemoteEndPoint?.ToString() ?? "Unknown";
    }

    public async Task HandleAsync()
    {
        try
        {
            Console.WriteLine($"📨 Started handling client: {ClientEndPoint}");

            while (_isConnected && _client.Connected)
            {
                try
                {
                    var message = await ReadMessageAsync();
                    if (message == null)
                    {
                        Console.WriteLine($"📨 {ClientEndPoint}: Received null message, closing connection");
                        break;
                    }

                    Console.WriteLine($"📨 From {ClientEndPoint}: {message.Action}");


                    var response = message.Action switch
                    {
                        "login" => await HandleLoginAsync(message),
                        "register" => await HandleRegisterAsync(message),
                        "send_message" => await HandleSendMessageAsync(message),
                        "get_messages" => await HandleGetMessagesAsync(message),
                        "get_chats" => await HandleGetChatsAsync(message),
                        "search_users" => await HandleSearchUsersAsync(message),
                        "create_group" => await HandleCreateGroupAsync(message),
                        "create_direct_chat" => await HandleCreateDirectChatAsync(message),
                        "get_contacts" => await HandleGetContactsAsync(message),
                        "add_contact" => await HandleAddContactAsync(message),
                        "get_user_profile" => await HandleGetUserProfileAsync(message),
                        "update_profile" => await HandleUpdateProfileAsync(message),
                        _ => new ServerMessage
                        {
                            Action = message.Action,
                            Success = false,
                            Error = $"Unknown action: {message.Action}"
                        }
                    };

                    if (response != null)
                    {
                        response.RequestId = message.RequestId;
                        await SendMessageToClientAsync(response);
                    }
                }
                catch (JsonException ex)
                {
                    Console.WriteLine($"❌ JSON parsing error from {ClientEndPoint}: {ex.Message}");
                    var errorResponse = new ServerMessage
                    {
                        Success = false,
                        Error = "Invalid JSON format"
                    };
                    await SendMessageToClientAsync(errorResponse);
                }
                catch (IOException ex)
                {
                    Console.WriteLine($"❌ IO error from {ClientEndPoint}: {ex.Message}");
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Error processing message from {ClientEndPoint}: {ex.Message}");
                    try
                    {
                        var errorResponse = new ServerMessage
                        {
                            Success = false,
                            Error = "Server error"
                        };
                        await SendMessageToClientAsync(errorResponse);
                    }
                    catch { }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error handling client {ClientEndPoint}: {ex.Message}");
        }
        finally
        {
            _isConnected = false;
            _stream?.Dispose();
            _client?.Dispose();
            _server.RemoveClient(this);
        }
    }

    private async Task<ServerMessage?> ReadMessageAsync()
    {
        try
        {
            byte[] buffer = new byte[8192];
            int bytesRead = await _stream.ReadAsync(buffer, 0, buffer.Length);

            if (bytesRead == 0)
            {
                Console.WriteLine($"📨 {ClientEndPoint}: Connection closed by client");
                return null;
            }

            string json = Encoding.UTF8.GetString(buffer, 0, bytesRead).Trim();

            if (string.IsNullOrEmpty(json))
            {
                Console.WriteLine($"❌ {ClientEndPoint}: Received empty message");
                return null;
            }

            Console.WriteLine($"📨 {ClientEndPoint}: Raw message: {json[..Math.Min(100, json.Length)]}...");

            var message = JsonSerializer.Deserialize<ServerMessage>(json);

            if (message == null)
            {
                Console.WriteLine($"❌ {ClientEndPoint}: Failed to deserialize message");
                return null;
            }

            return message;
        }
        catch (JsonException ex)
        {
            Console.WriteLine($"❌ {ClientEndPoint}: JSON error: {ex.Message}");
            throw;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ {ClientEndPoint}: Error reading message: {ex.Message}");
            return null;
        }
    }

    // ==================== HANDLERS ====================

    private async Task<ServerMessage> HandleLoginAsync(ServerMessage msg)
    {
        try
        {
            var loginData = JsonSerializer.Deserialize<LoginRequest>(msg.Data?.GetRawText() ?? "{}");
            if (loginData == null || string.IsNullOrEmpty(loginData.Username) || string.IsNullOrEmpty(loginData.Password))
            {
                Console.WriteLine($"❌ {ClientEndPoint}: Invalid login credentials");
                return ErrorResponse("Invalid credentials");
            }

            Console.WriteLine($"🔐 {ClientEndPoint}: Login attempt for user: {loginData.Username}");

            var user = await _authService.LoginAsync(loginData.Username, loginData.Password);
            if (user == null)
            {
                Console.WriteLine($"❌ {ClientEndPoint}: Invalid username or password");
                return ErrorResponse("Invalid username or password");
            }

            CurrentUserId = user.Id;

            // Оновлюємо статус на Online
            await _userService.SetUserStatusAsync(user.Id, "Online");

            Console.WriteLine($"✅ {ClientEndPoint}: User {loginData.Username} logged in successfully");

            return SuccessResponse("login", new
            {
                user_id = user.Id,
                username = user.Username,
                bio = user.Bio,
                birth_date = user.BirthDate,
                avatar_path = user.AvatarPath
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ {ClientEndPoint}: Login error: {ex.Message}");
            return ErrorResponse($"Login error: {ex.Message}");
        }
    }

    private async Task<ServerMessage> HandleRegisterAsync(ServerMessage msg)
    {
        try
        {
            var registerData = JsonSerializer.Deserialize<RegisterRequest>(msg.Data?.GetRawText() ?? "{}");
            if (registerData == null || string.IsNullOrEmpty(registerData.Username) || string.IsNullOrEmpty(registerData.Password))
            {
                Console.WriteLine($"❌ {ClientEndPoint}: Invalid registration data");
                return ErrorResponse("Invalid registration data");
            }

            Console.WriteLine($"📝 {ClientEndPoint}: Register attempt for user: {registerData.Username}");

            var user = await _authService.RegisterAsync(registerData.Username, registerData.Password);
            if (user == null)
            {
                Console.WriteLine($"❌ {ClientEndPoint}: User already exists");
                return ErrorResponse("User already exists");
            }

            CurrentUserId = user.Id;

            Console.WriteLine($"✅ {ClientEndPoint}: User {registerData.Username} registered successfully");

            return SuccessResponse("register", new
            {
                user_id = user.Id,
                username = user.Username
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ {ClientEndPoint}: Registration error: {ex.Message}");
            return ErrorResponse($"Registration error: {ex.Message}");
        }
    }

    private async Task<ServerMessage> HandleSendMessageAsync(ServerMessage msg)
    {
        if (CurrentUserId == 0)
            return ErrorResponse("Not authenticated");

        try
        {
            var requestData = JsonSerializer.Deserialize<SendMessageRequest>(msg.Data?.GetRawText() ?? "{}");

            if (requestData == null || string.IsNullOrEmpty(requestData.Text))
            {
                return ErrorResponse("Invalid message data");
            }

            var savedMsg = await _chatService.SendMessageAsync(
                requestData.ChatId,
                CurrentUserId,
                requestData.Text,
                requestData.Type);

            if (savedMsg != null)
            {
                var msgDto = new MessageDto
                {
                    Id = savedMsg.Id,
                    ChatId = savedMsg.ChatId,
                    SenderId = savedMsg.SenderId,
                    SenderUsername = savedMsg.Sender?.Username ?? "Unknown",
                    Text = savedMsg.Text,
                    SentAt = savedMsg.SentAt,
                    Status = savedMsg.Status.ToString()
                };

                var ackResponse = SuccessResponse("send_message", new { message_id = savedMsg.Id });
                await SendMessageToClientAsync(ackResponse);
                var broadcastMsg = SuccessResponse("new_message", msgDto);
                await _server.BroadcastToChatAsync(savedMsg.ChatId, broadcastMsg);

                return null;
            }

            return ErrorResponse("Could not save message");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ {ClientEndPoint}: Send message error: {ex.Message}");
            return ErrorResponse($"Send message error: {ex.Message}");
        }
    }

    private async Task<ServerMessage> HandleGetMessagesAsync(ServerMessage msg)
    {
        if (CurrentUserId == 0)
            return ErrorResponse("Not authenticated");

        try
        {
            var requestData = JsonSerializer.Deserialize<GetMessagesRequest>(msg.Data?.GetRawText() ?? "{}");
            if (requestData == null)
                return ErrorResponse("Invalid request");

            var messages = await _chatService.GetChatMessagesAsync(requestData.ChatId);
            var messageDtos = messages.Select(m => new MessageDto
            {
                Id = m.Id,
                ChatId = m.ChatId,
                SenderId = m.SenderId,
                SenderUsername = m.Sender?.Username ?? "Unknown",
                Text = m.Text,
                SentAt = m.SentAt,
                Status = m.Status.ToString()
            }).ToList();

            return SuccessResponse("get_messages", new { messages = messageDtos });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ {ClientEndPoint}: Get messages error: {ex.Message}");
            return ErrorResponse($"Get messages error: {ex.Message}");
        }
    }

    private async Task<ServerMessage> HandleGetChatsAsync(ServerMessage msg)
    {
        if (CurrentUserId == 0)
            return ErrorResponse("Not authenticated");

        try
        {
            var chats = await _chatService.GetUserChatsAsync(CurrentUserId);
            var chatDtos = chats.Select(c => new ChatDto
            {
                Id = c.Id,
                Name = c.Group?.Name ?? (c.Members.FirstOrDefault(m => m.UserId != CurrentUserId)?.User?.Username ?? "Unknown"),
                IsGroup = c.IsGroup,
                LastMessage = c.Messages.LastOrDefault()?.Text,
                LastMessageTime = c.Messages.LastOrDefault()?.SentAt,
                Members = c.Members.Select(m => new UserDto
                {
                    Id = m.User.Id,
                    Username = m.User.Username,
                    Bio = m.User.Bio,
                    BirthDate = m.User.BirthDate,
                    AvatarPath = m.User.AvatarPath,
                    Status = m.User.Status.ToString()
                }).ToList()
            }).ToList();

            return SuccessResponse("get_chats", new { chats = chatDtos });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ {ClientEndPoint}: Get chats error: {ex.Message}");
            return ErrorResponse($"Get chats error: {ex.Message}");
        }
    }

    private async Task<ServerMessage> HandleSearchUsersAsync(ServerMessage msg)
    {
        if (CurrentUserId == 0)
            return ErrorResponse("Not authenticated");

        try
        {
            var searchData = JsonSerializer.Deserialize<SearchUsersRequest>(msg.Data?.GetRawText() ?? "{}");
            if (searchData == null || string.IsNullOrEmpty(searchData.Query))
                return ErrorResponse("Invalid search query");

            var users = await _userService.SearchUsersAsync(searchData.Query);
            var userDtos = users.Select(u => new UserDto
            {
                Id = u.Id,
                Username = u.Username,
                Bio = u.Bio,
                BirthDate = u.BirthDate,
                AvatarPath = u.AvatarPath,
                Status = u.Status.ToString()
            }).ToList();

            return SuccessResponse("search_users", new { users = userDtos });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ {ClientEndPoint}: Search users error: {ex.Message}");
            return ErrorResponse($"Search users error: {ex.Message}");
        }
    }

    private async Task<ServerMessage> HandleCreateGroupAsync(ServerMessage msg)
    {
        if (CurrentUserId == 0)
            return ErrorResponse("Not authenticated");

        try
        {
            var groupData = JsonSerializer.Deserialize<CreateGroupRequest>(msg.Data?.GetRawText() ?? "{}");
            if (groupData == null || string.IsNullOrEmpty(groupData.Name))
                return ErrorResponse("Invalid group data");

            var group = await _chatService.CreateGroupAsync(groupData.Name, CurrentUserId, groupData.Description);
            if (group == null)
                return ErrorResponse("Failed to create group");

            return SuccessResponse("create_group", new
            {
                group_id = group.Id,
                chat_id = group.ChatId,
                name = group.Name,
                description = group.Description
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ {ClientEndPoint}: Create group error: {ex.Message}");
            return ErrorResponse($"Create group error: {ex.Message}");
        }
    }

    private async Task<ServerMessage> HandleCreateDirectChatAsync(ServerMessage msg)
    {
        if (CurrentUserId == 0)
            return ErrorResponse("Not authenticated");

        try
        {
            var data = JsonSerializer.Deserialize<JsonElement>(msg.Data?.GetRawText() ?? "{}");
            if (!data.TryGetProperty("contact_user_id", out var contactUserIdElem) || !contactUserIdElem.TryGetInt32(out int contactUserId))
                return ErrorResponse("Invalid contact user ID");

            var chat = await _chatService.GetOrCreateDirectChatAsync(CurrentUserId, contactUserId);
            if (chat == null)
                return ErrorResponse("Failed to create/get direct chat");

            return SuccessResponse("create_direct_chat", new
            {
                chat_id = chat.Id,
                is_group = chat.IsGroup
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ {ClientEndPoint}: Create direct chat error: {ex.Message}");
            return ErrorResponse($"Create direct chat error: {ex.Message}");
        }
    }

    private async Task<ServerMessage> HandleGetContactsAsync(ServerMessage msg)
    {
        if (CurrentUserId == 0)
            return ErrorResponse("Not authenticated");

        try
        {
            var contacts = await _userService.GetUserContactsAsync(CurrentUserId);
            var contactDtos = contacts.Select(c => new UserDto
            {
                Id = c.Id,
                Username = c.Username,
                Bio = c.Bio,
                BirthDate = c.BirthDate,
                AvatarPath = c.AvatarPath,
                Status = c.Status.ToString()
            }).ToList();

            return SuccessResponse("get_contacts", new { contacts = contactDtos });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ {ClientEndPoint}: Get contacts error: {ex.Message}");
            return ErrorResponse($"Get contacts error: {ex.Message}");
        }
    }

    private async Task<ServerMessage> HandleAddContactAsync(ServerMessage msg)
    {
        if (CurrentUserId == 0)
            return ErrorResponse("Not authenticated");

        try
        {
            var data = JsonSerializer.Deserialize<JsonElement>(msg.Data?.GetRawText() ?? "{}");
            if (!data.TryGetProperty("contact_user_id", out var contactUserIdElem) || !contactUserIdElem.TryGetInt32(out int contactUserId))
                return ErrorResponse("Invalid contact user ID");

            var contact = await _userService.AddContactAsync(CurrentUserId, contactUserId);
            return SuccessResponse("add_contact", new { contact_id = contact.Id });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ {ClientEndPoint}: Add contact error: {ex.Message}");
            return ErrorResponse($"Add contact error: {ex.Message}");
        }
    }

    private async Task<ServerMessage> HandleGetUserProfileAsync(ServerMessage msg)
    {
        try
        {
            var data = JsonSerializer.Deserialize<JsonElement>(msg.Data?.GetRawText() ?? "{}");
            if (!data.TryGetProperty("user_id", out var userIdElem) || !userIdElem.TryGetInt32(out int userId))
                return ErrorResponse("Invalid user ID");

            var user = await _userService.GetUserByIdAsync(userId);
            if (user == null)
                return ErrorResponse("User not found");

            return SuccessResponse("get_user_profile", new
            {
                id = user.Id,
                username = user.Username,
                bio = user.Bio,
                birth_date = user.BirthDate,
                avatar_path = user.AvatarPath,
                created_at = user.CreatedAt,
                status = user.Status.ToString()
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ {ClientEndPoint}: Get user profile error: {ex.Message}");
            return ErrorResponse($"Get user profile error: {ex.Message}");
        }
    }

    private async Task<ServerMessage> HandleUpdateProfileAsync(ServerMessage msg)
    {
        if (CurrentUserId == 0)
            return ErrorResponse("Not authenticated");

        try
        {
            var data = JsonSerializer.Deserialize<JsonElement>(msg.Data?.GetRawText() ?? "{}");

            string? bio = data.TryGetProperty("bio", out var bioElem) ? bioElem.GetString() : null;
            DateTime? birthDate = null;
            if (data.TryGetProperty("birth_date", out var birthDateElem) && birthDateElem.ValueKind == System.Text.Json.JsonValueKind.String)
            {
                if (DateTime.TryParse(birthDateElem.GetString(), out var parsed))
                    birthDate = parsed;
            }
            string? avatarPath = data.TryGetProperty("avatar_path", out var avatarElem) ? avatarElem.GetString() : null;

            var user = await _userService.UpdateUserProfileAsync(CurrentUserId, bio, birthDate, avatarPath);
            if (user == null)
                return ErrorResponse("Failed to update profile");

            return SuccessResponse("update_profile", new
            {
                id = user.Id,
                username = user.Username,
                bio = user.Bio,
                birth_date = user.BirthDate,
                avatar_path = user.AvatarPath
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ {ClientEndPoint}: Update profile error: {ex.Message}");
            return ErrorResponse($"Update profile error: {ex.Message}");
        }
    }

    // ==================== HELPERS ====================

    public async Task SendMessageAsync(object messageData)
    {
        try
        {
            string json = JsonSerializer.Serialize(messageData);
            byte[] buffer = Encoding.UTF8.GetBytes(json);
            await _stream.WriteAsync(buffer, 0, buffer.Length);
            await _stream.FlushAsync();
            Console.WriteLine($"📤 Sent to {ClientEndPoint}: {json[..Math.Min(50, json.Length)]}...");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Send error to {ClientEndPoint}: {ex.Message}");
        }
    }

    public async Task SendMessageToClientAsync(object messageData)
    {
        await SendMessageAsync(messageData);
    }

    private ServerMessage SuccessResponse(string action, object? data = null)
    {
        return new ServerMessage
        {
            Action = action,
            Data = data != null ? JsonSerializer.SerializeToElement(data) : null,
            Success = true
        };
    }

    private ServerMessage ErrorResponse(string error)
    {
        return new ServerMessage
        {
            Success = false,
            Error = error
        };
    }
}