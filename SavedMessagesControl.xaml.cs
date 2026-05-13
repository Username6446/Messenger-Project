using Messenger_Project.Data;
using Messenger_Project.Models;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
namespace Messenger_Project
{
    public partial class SavedMessagesControl : UserControl
    {
        private User _currentUser;
        private int _savedChatId; // ID чату "збережених"

        public SavedMessagesControl(User user)
        {
            InitializeComponent();
            _currentUser = user;

            // Знаходимо або створюємо чат "Збережені повідомлення"
            EnsureSavedMessagesChatExists();

            // Завантажуємо повідомлення на екран
            LoadMessages();
        }

        private void EnsureSavedMessagesChatExists()
        {
            using (var db = new AppDbContext())
            {
                // Шукаємо чат, де єдиний учасник (Members.Count == 1)
                var savedChat = db.Chats
                    .Include(c => c.Members)
                    .FirstOrDefault(c => c.IsGroup == false &&
                                         c.Members.Count == 1 &&
                                         c.Members.Any(m => m.UserId == _currentUser.Id));

                // Якщо такого чату ще немає — створюємо його
                if (savedChat == null)
                {
                    savedChat = new Chat { IsGroup = false };
                    db.Chats.Add(savedChat);
                    db.SaveChanges(); // Зберігаємо, щоб отримати Id чату

                    // Додаємо себе як єдиного учасника
                    var member = new ChatMember
                    {
                        ChatId = savedChat.Id,
                        UserId = _currentUser.Id
                    };
                    db.ChatMembers.Add(member);
                    db.SaveChanges();
                }

                // Запам'ятовуємо ID цього чату, щоб потім туди писати
                _savedChatId = savedChat.Id;
            }
        }

        private void LoadMessages()
        {
            using (var db = new AppDbContext())
            {
                // Дістаємо всі повідомлення з цього чату
                var messages = db.Messages
                    .Where(m => m.ChatId == _savedChatId)
                    .OrderBy(m => m.SentAt)
                    .ToList();

                SavedMessagesList.ItemsSource = messages;
            }
        }

        private void SaveMessage_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(MessageInput.Text))
                return;

            using (var db = new AppDbContext())
            {
                // Створюємо реальне повідомлення для БД
                var newMessage = new Message
                {
                    ChatId = _savedChatId,
                    SenderId = _currentUser.Id,
                    Text = MessageInput.Text,
                    SentAt = System.DateTime.UtcNow
                };

                db.Messages.Add(newMessage);
                db.SaveChanges(); // Відправляємо на somee.com!
            }

            MessageInput.Clear();
            LoadMessages();
        }

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            if (Window.GetWindow(this) is MainWindow mainWindow)
            {
                mainWindow.MainArea.Content = null;
                mainWindow.ChatView.Visibility = Visibility.Visible;
            }
        }
    }
}