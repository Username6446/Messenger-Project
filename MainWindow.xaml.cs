using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace Messenger_Project
{
    public partial class MainWindow : Window
    {
        private UserRecord _currentUser;
        
        // Тимчасові дані для відображення
        private List<DummyChat> _chats;

        public MainWindow(UserRecord user)
        {
            InitializeComponent();
            _currentUser = user;
            LoadMockData();
        }

        private void LoadMockData()
        {
            _chats = new List<DummyChat>
            {
                new DummyChat 
                { 
                    Name = "Global Team Chat", 
                    Messages = new List<DummyMessage> { new DummyMessage { Text = "Let's set up the database." } } 
                },
                new DummyChat 
                { 
                    Name = "Dev Mentor", 
                    Messages = new List<DummyMessage> { new DummyMessage { Text = "Stop writing logic in Code-Behind!" } } 
                }
            };

            ChatsListBox.ItemsSource = _chats;
        }

        private void OpenProfile_Click(object sender, RoutedEventArgs e)
        {
            ProfileWindow profileWindow = new ProfileWindow(_currentUser);
            profileWindow.Show();
        }

        private void ChatsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ChatsListBox.SelectedItem is DummyChat selectedChat)
            {
                // Оновлюємо ім'я чату зверху
                ActiveChatNameText.Text = selectedChat.Name;
                // Підтягуємо повідомлення обраного чату
                MessagesListBox.ItemsSource = selectedChat.Messages;
            }
        }
        // На перший час для чату але далі це треба буде переробляти
        private void SendMessage_Click(object sender, RoutedEventArgs e)
        {
            if (ChatsListBox.SelectedItem is DummyChat selectedChat && !string.IsNullOrWhiteSpace(MessageInput.Text))
            {
                selectedChat.Messages.Add(new DummyMessage { Text = MessageInput.Text });
                MessagesListBox.ItemsSource = null;
                MessagesListBox.ItemsSource = selectedChat.Messages;
                
                MessageInput.Clear();
            }
        }
    }

    // Тимчасові структури даних
    public class DummyChat
    {
        public string Name { get; set; }
        public List<DummyMessage> Messages { get; set; }
    }

    public class DummyMessage
    {
        public string Text { get; set; }
    }
}