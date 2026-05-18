using Messenger_Project.Models;
using Messenger_Project.Services;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows;
using System.Windows.Controls;
namespace Messenger_Project
{
    public partial class MainWindow : Window
    {
        private int _currentUserId;
        private string _currentUsername;
        private ServerService? _serverService;
        private User? _currentUserProfile;

        private List<ChatData> _chats = new();
        private ChatData? _selectedChat;

        // ObservableCollection для реактивного обновления UI
        private ObservableCollection<MessageDisplay> _messagesCollection = new();

        public MainWindow(int userId, string username)
        {
            InitializeComponent();
            _currentUserId = userId;
            _currentUsername = username;
            _serverService = App.ServerService;

            Title = $"Messenger - {username}";

            // Привязываем ObservableCollection к ListBox
            MessagesListBox.ItemsSource = _messagesCollection;

            LoadCurrentUserProfile();
            LoadChatsFromServerAsync();
            SetupServerEventHandlers();
        }

        private void SetupServerEventHandlers()
        {
            if (_serverService != null)
            {
                _serverService.OnMessageReceived += ServerService_OnMessageReceived;
                _serverService.OnConnectionStatusChanged += ServerService_OnConnectionStatusChanged;
            }
        }

        private void ServerService_OnConnectionStatusChanged(object? sender, string status)
        {
            this.Dispatcher.Invoke(() =>
            {
                Console.WriteLine($"📡 {status}");
            });
        }

        private void ServerService_OnMessageReceived(object? sender, ServerMessageEventArgs e)
        {
            this.Dispatcher.Invoke(() =>
            {
                // 🔴 КРИТИЧЕСКИЙ МОМЕНТ:
                // Если MainArea открыт (SavedMessages, Profile, etc) - ИГНОРИРУЕМ входящие сообщения
                // Только если ChatView видим (normale чаты), добавляем сообщение
                if (MainArea.Content != null || ChatView.Visibility != Visibility.Visible)
                {
                    return;
                }

                var msg = e.Message;
                if (msg == null || msg.Data == null) return;

                if (msg.Action == "new_message" && _selectedChat != null)
                {
                    try
                    {
                        var msgData = JsonSerializer.Deserialize<MessageDto>(msg.Data.Value.GetRawText());

                        // Добавляем сообщение только если оно пришло именно в текущий выбранный чат
                        if (msgData != null && msgData.ChatId == _selectedChat.Id)
                        {
                            var displayMsg = new MessageDisplay
                            {
                                Message = new MessageData
                                {
                                    Text = msgData.Text,
                                    SenderUsername = msgData.SenderUsername,
                                    SenderId = msgData.SenderId,
                                    IsOwn = msgData.SenderId == _currentUserId,
                                    SentAt = msgData.SentAt
                                }
                            };
                            _messagesCollection.Add(displayMsg);
                            MessagesListBox.ScrollIntoView(displayMsg);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error parsing message: {ex.Message}");
                    }
                }
            });
        }

        // ==================== LOADING FROM SERVER ====================

        private async Task LoadChatsFromServerAsync()
        {
            if (_serverService == null || !_serverService.IsConnected)
            {
                MessageBox.Show("❌ Not connected to server", "Error");
                return;
            }

            try
            {
                var (success, error, data) = await _serverService.GetChatsAsync();

                if (!success)
                {
                    MessageBox.Show($"❌ Failed to load chats: {error}", "Error");
                    return;
                }

                if (data == null || !data.HasValue)
                {
                    return;
                }

                _chats.Clear();

                var element = data.Value;
                if (element.TryGetProperty("chats", out var chatsElem))
                {
                    foreach (var chatElem in chatsElem.EnumerateArray())
                    {
                        if (chatElem.TryGetProperty("id", out var idElem) && idElem.TryGetInt32(out int chatId))
                        {
                            string chatName = "Unknown";
                            if (chatElem.TryGetProperty("name", out var nameElem))
                            {
                                chatName = nameElem.GetString() ?? "Unknown";
                            }

                            string? lastMessage = null;
                            if (chatElem.TryGetProperty("last_message", out var lastMsgElem))
                            {
                                lastMessage = lastMsgElem.GetString();
                            }

                            _chats.Add(new ChatData
                            {
                                Id = chatId,
                                Name = chatName,
                                LastMessage = lastMessage ?? "(No messages)"
                            });
                        }
                    }
                }

                RefreshChatsList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error loading chats: {ex.Message}");
            }
        }

        private async Task LoadChatMessagesFromServer(int chatId)
        {
            if (_serverService == null || !_serverService.IsConnected)
            {
                MessageBox.Show("❌ Not connected to server", "Error");
                return;
            }

            try
            {
                Console.WriteLine($"📨 Loading messages for chat {chatId}...");

                var (success, error, data) = await _serverService.GetMessagesAsync(chatId);

                if (!success || data == null || !data.HasValue)
                {
                    Console.WriteLine($"❌ Failed to load messages: {error}");
                    _messagesCollection.Clear();
                    return;
                }

                var messages = new List<MessageData>();

                var element = data.Value;
                if (element.TryGetProperty("messages", out var messagesElem))
                {
                    foreach (var msgElem in messagesElem.EnumerateArray())
                    {
                        try
                        {
                            if (msgElem.TryGetProperty("text", out var textElem) &&
                                msgElem.TryGetProperty("sender_id", out var senderIdElem) &&
                                senderIdElem.TryGetInt32(out int senderId) &&
                                msgElem.TryGetProperty("sender_username", out var senderNameElem))
                            {
                                DateTime sentAt = DateTime.UtcNow;
                                if (msgElem.TryGetProperty("sent_at", out var sentAtElem) &&
                                    sentAtElem.ValueKind == JsonValueKind.String)
                                {
                                    if (DateTime.TryParse(sentAtElem.GetString(), out var parsed))
                                        sentAt = parsed;
                                }

                                var msgData = new MessageData
                                {
                                    Text = textElem.GetString() ?? "",
                                    SenderUsername = senderNameElem.GetString() ?? "Unknown",
                                    SenderId = senderId,
                                    IsOwn = senderId == _currentUserId,
                                    SentAt = sentAt
                                };
                                messages.Add(msgData);

                                Console.WriteLine($"   📝 Loaded: [{msgData.SenderUsername}]: {msgData.Text}");
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"❌ Error parsing message: {ex.Message}");
                        }
                    }
                }

                Console.WriteLine($"✅ Loaded {messages.Count} messages total");

                // Очищаем и добавляем в ObservableCollection
                _messagesCollection.Clear();

                foreach (var msg in messages)
                {
                    var displayMsg = new MessageDisplay { Message = msg };
                    _messagesCollection.Add(displayMsg);
                }

                // Прокручиваем вниз
                if (_messagesCollection.Count > 0)
                {
                    MessagesListBox.ScrollIntoView(_messagesCollection[_messagesCollection.Count - 1]);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error loading messages: {ex.Message}");
            }
        }

        private async Task LoadCurrentUserProfile()
        {
            if (_serverService == null || !_serverService.IsConnected)
                return;

            try
            {
                var (success, error, data) = await _serverService.GetUserProfileAsync(_currentUserId);

                if (success && data != null && data.HasValue)
                {
                    var element = data.Value;
                    _currentUserProfile = new User
                    {
                        Id = _currentUserId,
                        Username = _currentUsername,
                        Bio = element.TryGetProperty("bio", out var bioElem) ? bioElem.GetString() : null,
                        BirthDate = element.TryGetProperty("birth_date", out var bdElem) && bdElem.ValueKind == JsonValueKind.String
                            ? DateTime.Parse(bdElem.GetString() ?? "")
                            : null,
                        AvatarPath = element.TryGetProperty("avatar_path", out var avatarElem) ? avatarElem.GetString() : null,
                        CreatedAt = element.TryGetProperty("created_at", out var createdElem) && createdElem.ValueKind == JsonValueKind.String
                            ? DateTime.Parse(createdElem.GetString() ?? DateTime.UtcNow.ToString())
                            : DateTime.UtcNow,
                        Status = element.TryGetProperty("status", out var statusElem) ? statusElem.GetString() : "Offline"
                    };
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error loading user profile: {ex.Message}");
            }
        }

        // ==================== UI UPDATES ====================

        private void RefreshChatsList()
        {
            ChatsListBox.ItemsSource = null;
            ChatsListBox.ItemsSource = _chats.Select(c => new { c.Id, c.Name, c.LastMessage }).ToList();
        }

        // ==================== EVENT HANDLERS ====================

        private async void ChatsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedItem = ChatsListBox.SelectedItem as dynamic;
            if (selectedItem != null)
            {
                int chatId = selectedItem.Id;
                string chatName = selectedItem.Name;

                _selectedChat = _chats.FirstOrDefault(c => c.Id == chatId);
                if (_selectedChat != null)
                {
                    ActiveChatNameText.Text = chatName;
                    Console.WriteLine($"📨 Selected chat: {chatName} (ID: {chatId})");
                    await LoadChatMessagesFromServer(chatId);
                }
            }
        }

        private async void SendMessage_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedChat == null || string.IsNullOrWhiteSpace(MessageInput.Text))
                return;

            string text = MessageInput.Text;
            MessageInput.Clear();

            if (_serverService == null || !_serverService.IsConnected)
            {
                MessageBox.Show("❌ Not connected to server", "Error");
                MessageInput.Text = text;
                return;
            }

            try
            {
                Console.WriteLine($"📨 Sending: {text}");
                var (success, error, _) = await _serverService.SendMessageAsync(_selectedChat.Id, text);

                if (!success)
                {
                    MessageBox.Show($"❌ Failed to send: {error}", "Error");
                    MessageInput.Text = text;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ Error: {ex.Message}", "Error");
                MessageInput.Text = text;
            }
        }

        public async Task OpenDirectChatWithUser(User user)
        {
            if (_serverService == null || !_serverService.IsConnected)
                return;

            try
            {
                var (success, error, data) = await _serverService.CreateDirectChatAsync(user.Id);

                if (!success)
                {
                    MessageBox.Show($"❌ Error: {error}");
                    return;
                }

                MainArea.Content = null;
                ChatView.Visibility = Visibility.Visible;

                await LoadChatsFromServerAsync();

                if (data != null && data.Value.TryGetProperty("chat_id", out var idElem))
                {
                    int newChatId = idElem.GetInt32();
                    var chatToSelect = _chats.FirstOrDefault(c => c.Id == newChatId);

                    if (chatToSelect != null)
                    {
                        _selectedChat = chatToSelect;
                        ActiveChatNameText.Text = chatToSelect.Name;
                        await LoadChatMessagesFromServer(newChatId);

                        ChatsListBox.SelectedItem = ChatsListBox.Items
                            .Cast<dynamic>()
                            .FirstOrDefault(item => item.Id == newChatId);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ Error: {ex.Message}");
            }
        }

        private void OpenSideBar_Click(object sender, RoutedEventArgs e)
        {
            SideBar.Visibility = Visibility.Visible;
        }

        private void CloseSideBar_Click(object sender, RoutedEventArgs e)
        {
            SideBar.Visibility = Visibility.Collapsed;
        }

        private void Settings_Click(object sender, RoutedEventArgs e)
        {
            MainArea.Content = new SettingsControl();
            ChatView.Visibility = Visibility.Collapsed;
            SideBar.Visibility = Visibility.Collapsed;
        }

        private void Profile_Click(object sender, RoutedEventArgs e)
        {
            if (_currentUserProfile != null)
            {
                MainArea.Content = new ProfileControl(_currentUserProfile);
                ChatView.Visibility = Visibility.Collapsed;
                SideBar.Visibility = Visibility.Collapsed;
            }
            else
            {
                MessageBox.Show("❌ Unable to load profile", "Error");
            }
        }

        private void Contacts_Click(object sender, RoutedEventArgs e)
        {
            MainArea.Content = new ContactsControl(_currentUserId, _serverService);
            ChatView.Visibility = Visibility.Collapsed;
            SideBar.Visibility = Visibility.Collapsed;
        }

        private void SavedMessages_Click(object sender, RoutedEventArgs e)
        {
            // 1. Сбрасываем выбранный чат
            _selectedChat = null;

            // 2. Скрываем стандартную панель чата
            ChatView.Visibility = Visibility.Collapsed;
            SideBar.Visibility = Visibility.Collapsed;

            // 3. Открываем SavedMessagesControl
            MainArea.Content = new SavedMessagesControl(_serverService, _currentUserId);
        }

        private void NewGroup_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Create group coming soon!");
        }

        private void BackToChats_Click(object sender, RoutedEventArgs e)
        {
            MainArea.Content = null;
            ChatView.Visibility = Visibility.Visible;
            SideBar.Visibility = Visibility.Collapsed;
        }

        private async void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            string searchText = SearchBox.Text.Trim();

            if (string.IsNullOrEmpty(searchText))
                return;

            if (_serverService == null || !_serverService.IsConnected)
            {
                MessageBox.Show("❌ Not connected to server", "Error");
                return;
            }

            try
            {
                var (success, error, data) = await _serverService.SearchUsersAsync(searchText);

                if (!success)
                {
                    MessageBox.Show($"❌ Search failed: {error}", "Error");
                    return;
                }

                if (data == null || !data.HasValue)
                {
                    MessageBox.Show("❌ No results", "Info");
                    return;
                }

                var element = data.Value;
                var foundUsers = new List<User>();

                if (element.TryGetProperty("users", out var usersElem))
                {
                    foreach (var userElem in usersElem.EnumerateArray())
                    {
                        var user = new User();

                        if (userElem.TryGetProperty("id", out var idElem))
                            user.Id = idElem.GetInt32();

                        if (userElem.TryGetProperty("username", out var usernameElem))
                            user.Username = usernameElem.GetString() ?? "Unknown";

                        if (userElem.TryGetProperty("bio", out var bioElem) && bioElem.ValueKind != JsonValueKind.Null)
                            user.Bio = bioElem.GetString() ?? "";

                        if (userElem.TryGetProperty("birth_date", out var birthDateElem) && birthDateElem.ValueKind == JsonValueKind.String)
                        {
                            string dateStr = birthDateElem.GetString() ?? "";
                            if (DateTime.TryParse(dateStr, out DateTime parsedDate))
                            {
                                user.BirthDate = parsedDate;
                            }
                        }

                        if (userElem.TryGetProperty("avatar_path", out var avatarElem))
                            user.AvatarPath = avatarElem.GetString();

                        if (userElem.TryGetProperty("status", out var statusElem))
                            user.Status = statusElem.GetString();

                        // 🔴 Не показываем своего пользователя в результатах
                        if (user.Id != _currentUserId)
                        {
                            foundUsers.Add(user);
                        }
                    }
                }

                if (foundUsers.Count == 0)
                {
                    MessageBox.Show("❌ No users found (excluding yourself)", "Info");
                    return;
                }

                ChatView.Visibility = Visibility.Collapsed;
                MainArea.Content = new SearchResultsControl(foundUsers, _serverService, this);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ Search error: {ex.Message}", "Error");
            }
        }
    }

    // ==================== DATA CLASSES ====================

    public class MessageDisplay
    {
        public MessageData Message { get; set; } = new();
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

    public class ChatData
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string LastMessage { get; set; } = string.Empty;
    }

    public class MessageData
    {
        public string Text { get; set; } = string.Empty;
        public string SenderUsername { get; set; } = string.Empty;
        public int SenderId { get; set; }
        public bool IsOwn { get; set; }
        public DateTime SentAt { get; set; } = DateTime.UtcNow;

        public string SentAtFormatted
        {
            get
            {
                return SentAt.ToString("HH:mm");
            }
        }
    }
}