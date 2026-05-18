using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using Messenger_Project.Models;
using Messenger_Project.Services;

namespace Messenger_Project
{
    public partial class ContactsControl : UserControl
    {
        private int _currentUserId;
        private ServerService? _serverService;
        private List<User> _allContacts = new();
        private MainWindow? _mainWindow;

        public ContactsControl()
        {
            InitializeComponent();
        }

        public ContactsControl(int currentUserId, ServerService? serverService)
        {
            InitializeComponent();
            _currentUserId = currentUserId;
            _serverService = serverService;
            _mainWindow = Window.GetWindow(this) as MainWindow;

            Loaded += async (s, e) => await LoadContactsAsync();
        }

        private async System.Threading.Tasks.Task LoadContactsAsync()
        {
            if (_serverService == null || !_serverService.IsConnected)
            {
                MessageBox.Show("❌ Not connected to server", "Error");
                return;
            }

            try
            {
                var (success, error, data) = await _serverService.GetContactsAsync();

                if (success && data.HasValue && data.Value.TryGetProperty("contacts", out var contactsElem))
                {
                    _allContacts.Clear();

                    foreach (var contactElem in contactsElem.EnumerateArray())
                    {
                        var user = new User();

                        if (contactElem.TryGetProperty("id", out var idElem))
                            user.Id = idElem.GetInt32();

                        if (contactElem.TryGetProperty("username", out var usernameElem))
                            user.Username = usernameElem.GetString() ?? "Unknown";

                        if (contactElem.TryGetProperty("bio", out var bioElem) && bioElem.ValueKind != JsonValueKind.Null)
                            user.Bio = bioElem.GetString() ?? "";

                        if (contactElem.TryGetProperty("birth_date", out var birthDateElem) &&
                            birthDateElem.ValueKind == JsonValueKind.String)
                        {
                            string dateStr = birthDateElem.GetString() ?? "";
                            if (DateTime.TryParse(dateStr, out DateTime parsedDate))
                                user.BirthDate = parsedDate;
                        }

                        if (contactElem.TryGetProperty("avatar_path", out var avatarElem))
                            user.AvatarPath = avatarElem.GetString();

                        if (contactElem.TryGetProperty("status", out var statusElem))
                            user.Status = statusElem.GetString();

                        _allContacts.Add(user);
                    }

                    RefreshContactsList(_allContacts);
                }
                else
                {
                    ShowEmptyState();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error loading contacts: {ex.Message}");
                ShowEmptyState();
            }
        }

        private void RefreshContactsList(List<User> contacts)
        {
            if (contacts.Count == 0)
            {
                ShowEmptyState();
            }
            else
            {
                EmptyState.Visibility = Visibility.Collapsed;
                ContactsListBox.ItemsSource = null;
                ContactsListBox.ItemsSource = contacts;
                CountText.Text = $"({contacts.Count})";
            }
        }

        private void ShowEmptyState()
        {
            ContactsListBox.ItemsSource = null;
            EmptyState.Visibility = Visibility.Visible;
            CountText.Text = "(0)";
        }

        private void ViewProfile_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is User contactUser)
            {
                if (Window.GetWindow(this) is MainWindow mainWindow)
                {
                    // Pass current user ID and service so profile knows this is not the current user
                    mainWindow.MainArea.Content = new ProfileControl(contactUser, _currentUserId, _serverService);
                }
            }
        }

        private async void OpenChat_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is User contactUser)
            {
                if (Window.GetWindow(this) is MainWindow mainWindow)
                {
                    // Use the existing method to open or create a direct chat with the user
                    await mainWindow.OpenDirectChatWithUser(contactUser);
                }
            }
        }

        private async void RemoveContact_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button?.DataContext is User user)
            {
                var result = MessageBox.Show($"Remove {user.Username} from contacts?",
                    "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    await RemoveContactAsync(user.Id);
                }
            }
        }

        private async System.Threading.Tasks.Task RemoveContactAsync(int contactUserId)
        {
            if (_serverService == null || !_serverService.IsConnected)
                return;

            try
            {
                var contactToRemove = _allContacts.FirstOrDefault(c => c.Id == contactUserId);
                if (contactToRemove != null)
                {
                    _allContacts.Remove(contactToRemove);
                    RefreshContactsList(_allContacts);
                    MessageBox.Show("✅ Contact removed", "Success");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ Error: {ex.Message}", "Error");
            }
        }

        private void Search_Click(object sender, RoutedEventArgs e)
        {
            string searchText = SearchBox.Text.Trim().ToLower();

            if (string.IsNullOrEmpty(searchText))
            {
                RefreshContactsList(_allContacts);
                return;
            }

            var filtered = _allContacts
                .Where(c => c.Username.ToLower().Contains(searchText))
                .ToList();

            RefreshContactsList(filtered);
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