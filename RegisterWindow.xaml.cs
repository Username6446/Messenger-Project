using System;
using System.Collections.Generic;
using System.Windows;
using BCrypt.Net;
using Messenger_Project.Data;
using Messenger_Project.Models;

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

            try
            {
                using (var db = new AppDbContext())
                {
                    // Перевірка чи є таке ім'я у базі 
                    bool userExists = db.Users.Any(u => u.Username.ToLower() == username.ToLower());

                    if (userExists)
                    {
                        ShowError("This username is already busy. Choose another.");
                        return;
                    }

                    // Хешування паролю
                    string passwordHash = BCrypt.Net.BCrypt.HashPassword(password);

                    //Створення нового користувача
                    var newUser = new User
                    {
                        Username = username,
                        PasswordHash = passwordHash,
                        CreatedAt = DateTime.UtcNow
                    };

                    //Запис у базу
                    db.Users.Add(newUser);
                    db.SaveChanges();

                    MainWindow mainWindow = new MainWindow(newUser);
                    mainWindow.Show();
                    this.Close();
                }
            }
            catch (Exception ex)
            {
                ShowError($"Database connection failed: {ex.Message}");
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
                ShowError("The username must contain a minimum of 3 characters.");
                UsernameBox.Focus();
                return false;
            }

            if (string.IsNullOrEmpty(password))
            {
                ShowError("Enter the password.");
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
}