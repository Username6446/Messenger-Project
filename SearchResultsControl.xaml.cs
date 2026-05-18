using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using Messenger_Project.Models;
using Messenger_Project.Services;

namespace Messenger_Project
{
    public partial class SearchResultsControl : UserControl
    {
        private List<User> _foundUsers;
        private ServerService? _serverService;
        private MainWindow? _mainWindow;

        public SearchResultsControl(List<User> foundUsers, ServerService? serverService, MainWindow? mainWindow)
        {
            InitializeComponent();
            _foundUsers = foundUsers;
            _serverService = serverService;
            _mainWindow = mainWindow;
            ResultsListBox.ItemsSource = _foundUsers;
        }

        private void ViewProfile_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button?.DataContext is User user && _mainWindow != null)
            {

                _mainWindow.MainArea.Content = new ProfileControl(user, _serverService?.CurrentUserId ?? 0, _serverService);
            }
        }

        private async void SendMessage_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button?.DataContext is User user && _mainWindow != null)
            {
                await _mainWindow.OpenDirectChatWithUser(user);
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            if (Window.GetWindow(this) is MainWindow mainWindow)
            {
                mainWindow.MainArea.Content = null;
                mainWindow.ChatView.Visibility = Visibility.Visible;
            }
        }
    }
}