using System.Windows;
using System.Windows.Controls;
using System.Collections.Generic;


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
        private void Sign_In_Button_Click(object sender, RoutedEventArgs e)
        {
            HideError();

            string username = UsernameBox.Text.Trim();
            string password = PasswordBox.Password;

            // базова перевірка
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

            // перевірка на існуючий акаунт
            var user = UserDatabase.FindUser(username, password);

            if (user == null)
            {
                ShowError("Invalid username or password");
                return;
            }


            MainWindow mainWindow = new MainWindow(user);
            mainWindow.Show();
            this.Close();
        }

        public static class UserDatabase
        {
            private static List<UserRecord> _users = new List<UserRecord>();

            public static bool UserExists(string username)
            {
                return _users.Exists(u =>
                    u.Username.Equals(username, System.StringComparison.OrdinalIgnoreCase));
            }

            public static void AddUser(string username, string password)
            {
                _users.Add(new UserRecord
                {
                    Username = username,
                    Password = password
                });
            }

            public static UserRecord? FindUser(string username, string password)
            {
                return _users.Find(u =>
                    u.Username.Equals(username, System.StringComparison.OrdinalIgnoreCase) &&
                    u.Password == password);
            }
        }
    }
}
