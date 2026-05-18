using System;
using System.Windows;
using Messenger_Project.Services;

namespace Messenger_Project
{
    public partial class RegisterWindow : Window
    {
        public RegisterWindow()
        {
            InitializeComponent();
        }

        private async void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            HideError();

            string username = UsernameBox.Text.Trim();
            string password = PasswordBox.Password;
            string repeatPassword = RepeatPasswordBox.Password;

            // базова валідація
            if (!ValidateInputs(username, password, repeatPassword))
                return;

            try
            {
                var serverService = App.ServerService;

                // Проверяем подключение к серверу
                if (serverService == null || !serverService.IsConnected)
                {
                    ShowError("❌ Not connected to server.\n\nMake sure server is running on localhost:5000");
                    return;
                }

                // ==================== REGISTER ЧЕРЕЗ СЕРВЕР ====================
                var (success, error, data) = await serverService.RegisterAsync(username, password);

                if (!success)
                {
                    ShowError($"❌ {error ?? "Registration failed"}");
                    return;
                }

                if (data == null || !data.HasValue)
                {
                    ShowError("❌ Invalid server response");
                    return;
                }

                // Парсимо ответ от сервера
                var element = data.Value;

                if (!element.TryGetProperty("user_id", out var userIdElem) ||
                    !userIdElem.TryGetInt32(out int userId))
                {
                    ShowError("❌ Invalid user data from server");
                    return;
                }

                // Сохраняем ID пользователя
                serverService.SetCurrentUserId(userId, username);

                // Открываем главное окно
                MainWindow mainWindow = new MainWindow(userId, username);
                mainWindow.Show();
                this.Close();
            }
            catch (Exception ex)
            {
                ShowError($"{ex.Message}");
            }
        }

        private bool ValidateInputs(string username, string password, string repeatPassword)
        {
            if (string.IsNullOrEmpty(username))
            {
                ShowError("Enter a username");
                UsernameBox.Focus();
                return false;
            }

            if (username.Length < 3)
            {
                ShowError("Username must be at least 3 characters");
                UsernameBox.Focus();
                return false;
            }

            if (string.IsNullOrEmpty(password))
            {
                ShowError("Enter a password");
                PasswordBox.Focus();
                return false;
            }

            if (password.Length < 6)
            {
                ShowError("Password must be at least 6 characters");
                PasswordBox.Focus();
                return false;
            }

            if (string.IsNullOrEmpty(repeatPassword))
            {
                ShowError("Repeat the password");
                RepeatPasswordBox.Focus();
                return false;
            }

            if (password != repeatPassword)
            {
                ShowError("Passwords do not match");
                RepeatPasswordBox.Focus();
                return false;
            }

            return true;
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
    }
}
