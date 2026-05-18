using System.Windows;
using Messenger_Project.Services;

namespace Messenger_Project
{
    public partial class LoginWindow : Window
    {
        public LoginWindow()
        {
            InitializeComponent();
        }

        private void ShowError(string message)
        {
            ErrorText.Text = message;
            ErrorText.Visibility = Visibility.Visible;
        }

        private void HideError()
        {
            ErrorText.Visibility = Visibility.Collapsed;
            ErrorText.Text = string.Empty;
        }

        private async void Sign_In_Button_Click(object sender, RoutedEventArgs e)
        {
            HideError();

            string username = UsernameBox.Text.Trim();
            string password = PasswordBox.Password;

            if (string.IsNullOrEmpty(username))
            {
                ShowError("Enter a username");
                UsernameBox.Focus();
                return;
            }

            if (string.IsNullOrEmpty(password))
            {
                ShowError("Enter the password");
                PasswordBox.Focus();
                return;
            }

            try
            {
                var serverService = App.ServerService;

                if (serverService == null || !serverService.IsConnected)
                {
                    ShowError($"❌ Not connected to server.\n\nMake sure server is running on {ServerService.ServerHost}:{ServerService.ServerPort}");
                    return;
                }

                var (success, error, data) = await serverService.LoginAsync(username, password);

                if (!success)
                {
                    ShowError($"❌ {error ?? "Login failed"}");
                    return;
                }

                if (data == null || !data.HasValue)
                {
                    ShowError("❌ Invalid server response");
                    return;
                }

                var element = data.Value;

                if (!element.TryGetProperty("user_id", out var userIdElem) ||
                    !userIdElem.TryGetInt32(out int userId))
                {
                    ShowError("❌ Invalid user data from server");
                    return;
                }

                string? userUsername = null;
                if (element.TryGetProperty("username", out var usernameElem))
                {
                    userUsername = usernameElem.GetString();
                }

                serverService.SetCurrentUserId(userId, userUsername ?? username);

                MainWindow mainWindow = new MainWindow(userId, username);
                mainWindow.Show();
                this.Close();
            }
            catch (Exception ex)
            {
                ShowError($"❌ Error: {ex.Message}");
            }
        }
    }
}