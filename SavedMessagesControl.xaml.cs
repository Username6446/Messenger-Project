using Messenger_Project.Models;
using Messenger_Project.Services;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Messenger_Project
{
    public partial class SavedMessagesControl : UserControl
    {
        private readonly ServerService? _serverService;
        private int _savedChatId;
        private int _currentUserId;

        private ObservableCollection<MessageDisplay> _messagesCollection = new();

        public SavedMessagesControl()
        {
            InitializeComponent();
            _serverService = App.ServerService;
            _currentUserId = _serverService?.CurrentUserId ?? 0;
            SavedMessagesList.ItemsSource = _messagesCollection;

            Loaded += async (s, e) => await InitializeSavedChatAsync();

            Unloaded += (s, e) => {
                _messagesCollection.Clear();
                _savedChatId = 0;
            };
        }

        public SavedMessagesControl(ServerService? serverService, int currentUserId)
        {
            InitializeComponent();
            _serverService = serverService;
            _currentUserId = currentUserId;
            SavedMessagesList.ItemsSource = _messagesCollection;

            Loaded += async (s, e) => await InitializeSavedChatAsync();

            Unloaded += (s, e) => {
                _messagesCollection.Clear();
                _savedChatId = 0;
            };
        }

        private async Task InitializeSavedChatAsync()
        {
            if (_serverService == null || !_serverService.IsConnected)
            {
                MessageBox.Show("❌ Not connected to server");
                return;
            }

            try
            {
                var (success, error, data) = await _serverService.CreateDirectChatAsync(_currentUserId);

                if (success && data.HasValue && data.Value.TryGetProperty("chat_id", out var idElem))
                {
                    _savedChatId = idElem.GetInt32();
                    Console.WriteLine($"✅ Opened Saved Messages chat (ID: {_savedChatId})");
                    await LoadMessagesFromServerAsync();
                }
                else
                {
                    MessageBox.Show($"❌ Failed to open Saved Messages: {error}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error initializing Saved Messages: {ex.Message}");
            }
        }

        private async Task LoadMessagesFromServerAsync()
        {
            if (_savedChatId == 0 || _serverService == null)
                return;

            var (success, error, data) = await _serverService.GetMessagesAsync(_savedChatId);

            if (success && data.HasValue && data.Value.TryGetProperty("messages", out var messagesElem))
            {
                _messagesCollection.Clear();
                foreach (var msgElem in messagesElem.EnumerateArray())
                {
                    try
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
                            Text = msgElem.GetProperty("text").GetString() ?? "",
                            SenderUsername = msgElem.GetProperty("sender_username").GetString() ?? "You",
                            SenderId = msgElem.GetProperty("sender_id").GetInt32(),
                            IsOwn = true,
                            SentAt = sentAt
                        };
                        _messagesCollection.Add(new MessageDisplay { Message = msgData });
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error parsing saved message: {ex.Message}");
                    }
                }

                if (_messagesCollection.Count > 0)
                    SavedMessagesList.ScrollIntoView(_messagesCollection.Last());
            }
        }

        private async void SaveMessage_Click(object sender, RoutedEventArgs e)
        {
            string text = MessageInput.Text.Trim();
            if (string.IsNullOrEmpty(text) || _serverService == null || _savedChatId == 0)
                return;

            MessageInput.Clear();

            var (success, error, _) = await _serverService.SendMessageAsync(_savedChatId, text);

            if (success)
            {
                await Task.Delay(100);
                await LoadMessagesFromServerAsync();
            }
            else
            {
                MessageBox.Show($"❌ Failed to save: {error}");
                MessageInput.Text = text;
            }
        }

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            if (Window.GetWindow(this) is MainWindow mainWindow)
            {
                mainWindow.MainArea.Content = null;
                mainWindow.ChatView.Visibility = Visibility.Visible;
                _messagesCollection.Clear();
                _savedChatId = 0;
            }
        }
    }
}