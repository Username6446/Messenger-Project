using System;
using System.Windows;
using System.Windows.Controls;
using Messenger_Project.Models;
using Messenger_Project.Services;

namespace Messenger_Project
{
    public partial class ProfileControl : UserControl
    {
        private User _currentUser;
        private User _profileUser; // Пользователь, чей профиль мы смотрим
        private int _currentUserId;
        private ServerService? _serverService;
        private bool _isOwnProfile = false;

        public ProfileControl()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Конструктор для просмотра своего профиля
        /// </summary>
        public ProfileControl(User user)
        {
            InitializeComponent();
            _profileUser = user;
            _currentUser = user;
            _serverService = App.ServerService;
            _currentUserId = _serverService?.CurrentUserId ?? 0;
            _isOwnProfile = true;

            LoadProfile(user, isOwnProfile: true);
        }

        /// <summary>
        /// Конструктор для просмотра профиля другого пользователя
        /// </summary>
        public ProfileControl(User user, int currentUserId, ServerService? serverService)
        {
            InitializeComponent();
            _profileUser = user;
            _currentUserId = currentUserId;
            _serverService = serverService;
            _isOwnProfile = false;

            LoadProfile(user, isOwnProfile: false);
            Loaded += async (s, e) => await CheckIfContactAsync();
        }

        private void LoadProfile(User user, bool isOwnProfile)
        {
            DisplayUsername.Text = user.Username;
            UsernameText.Text = user.Username;
            BioText.Text = string.IsNullOrEmpty(user.Bio) ? "No bio yet" : user.Bio;

            // Показываем статус с цветом
            string statusText = user.Status ?? "Offline";
            StatusText.Text = $"● {statusText}";

            // Зеленый для Online, серый для Offline
            if (statusText == "Online")
                StatusText.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.LimeGreen);
            else
                StatusText.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Gray);

            if (user.BirthDate.HasValue)
                BirthDateText.Text = user.BirthDate.Value.ToShortDateString();
            else
                BirthDateText.Text = "Not specified";

            MemberSinceText.Text = user.CreatedAt.ToString("MMMM dd, yyyy", System.Globalization.CultureInfo.InvariantCulture);

            // Показываем кнопку редактирования только для своего профиля
            if (isOwnProfile)
            {
                EditProfileButton.Visibility = Visibility.Visible;
                AddContactButton.Visibility = Visibility.Collapsed;
            }
            else
            {
                EditProfileButton.Visibility = Visibility.Collapsed;
                AddContactButton.Visibility = Visibility.Visible;
            }
        }

        private async System.Threading.Tasks.Task CheckIfContactAsync()
        {
            if (_serverService == null || !_serverService.IsConnected)
                return;

            try
            {
                // Получаем список контактов текущего пользователя
                var (success, error, data) = await _serverService.GetContactsAsync();

                if (success && data.HasValue && data.Value.TryGetProperty("contacts", out var contactsElem))
                {
                    foreach (var contactElem in contactsElem.EnumerateArray())
                    {
                        if (contactElem.TryGetProperty("id", out var idElem) && idElem.TryGetInt32(out int contactId))
                        {
                            if (contactId == _profileUser.Id)
                            {
                                // Пользователь уже в контактах - скрываем кнопку
                                AddContactButton.Visibility = Visibility.Collapsed;
                                return;
                            }
                        }
                    }
                }

                // Если достигли сюда - контакта нет, показываем кнопку
                AddContactButton.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error checking contacts: {ex.Message}");
            }
        }

        private void EditProfile_Click(object sender, RoutedEventArgs e)
        {
            if (Window.GetWindow(this) is MainWindow mainWindow && _isOwnProfile)
            {
                mainWindow.MainArea.Content = new EditProfileControl(_profileUser, _serverService, this);
            }
        }

        private async void AddContact_Click(object sender, RoutedEventArgs e)
        {
            if (_serverService == null || !_serverService.IsConnected)
            {
                MessageBox.Show("❌ Not connected to server", "Error");
                return;
            }

            try
            {
                var (success, error, data) = await _serverService.AddContactAsync(_profileUser.Id);

                if (success)
                {
                    MessageBox.Show($"✅ {_profileUser.Username} added to contacts!", "Success");
                    AddContactButton.Visibility = Visibility.Collapsed;
                }
                else
                {
                    MessageBox.Show($"❌ Failed to add contact: {error}", "Error");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ Error: {ex.Message}", "Error");
            }
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