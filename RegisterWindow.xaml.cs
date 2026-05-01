using System.Collections.Generic;
using System.Windows;

namespace Messenger_Project
{
    public partial class RegisterWindow : Window
    {
        public RegisterWindow()
        {
            InitializeComponent();
        }

        private void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            HideError();

            string username = UsernameBox.Text.Trim();
            string password = PasswordBox.Password;
            string repeatPassword = RepeatPasswordBox.Password;

            // проста валідація, коли перенесем це все в бд треба буде переробити

            if (!ValidateInputs(username, password, repeatPassword))
                return;


            // ЗАГЛУШКА
            if (UserDatabase.UserExists(username))
            {
                ShowError("This username is already busy. Choose another.");
                return;
            }


            // ЗАГЛУШКА
            UserDatabase.AddUser(username, password);

            MainWindow mainWindow = new MainWindow();
            mainWindow.Show();
            this.Close();
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
                ShowError("The username must contain a minimum of 3 characters.");
                UsernameBox.Focus();
                return false;
            }

            if (string.IsNullOrEmpty(password))
            {
                ShowError("enter the password.");
                PasswordBox.Focus();
                return false;
            }

            if (password.Length < 6)
            {
                ShowError("Password must be at least 6 characters long.");
                PasswordBox.Focus();
                return false;
            }

            if (string.IsNullOrEmpty(repeatPassword))
            {
                ShowError("Repeat the password.");
                RepeatPasswordBox.Focus();
                return false;
            }

            if (password != repeatPassword)
            {
                ShowError("The passwords do not match.");
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





    // ЗАГЛУШКА -------- для того хто буде робить логін можете просто скопіювати цю умовну бд для тесту
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


    public class UserRecord
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
    // ЗАГЛУШКА -------- для того хто буде робить логін можете просто скопіювати цю умовну бд для тесту
}
