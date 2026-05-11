using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Messenger_Project.Data;
using Messenger_Project.Models;
namespace Messenger_Project
{
    public partial class StartWindow : Window
    {
        public StartWindow()
        {
            InitializeComponent();
        }

        private void Login_Button_Click(object sender, RoutedEventArgs e)
        {
            // Для нормального входу
            LoginWindow loginWindow = new LoginWindow();
            loginWindow.Show();
            this.Close();

            // Швидкий вхід 
            //try
            //{
            //    using (var db = new AppDbContext())
            //    {
            //        string testUsername = "qwerty";
            //        string testPassword = "qwerty123";

            //        var user = db.Users.FirstOrDefault(u => u.Username == testUsername);

            //        if (user == null)
            //        {
            //            user = new User
            //            {
            //                Username = testUsername,
            //                PasswordHash = BCrypt.Net.BCrypt.HashPassword(testPassword),
            //                CreatedAt = DateTime.UtcNow
            //            };
            //            db.Users.Add(user);
            //            db.SaveChanges();
            //        }
            //        MainWindow mainWindow = new MainWindow(user);
            //        mainWindow.Show();
            //        this.Close();
            //    }
            //}
            //catch (Exception ex)
            //{
            //    MessageBox.Show($"Database connection failed: {ex.Message}");
            //}
        }

        private void Register_Button_Click(object sender, RoutedEventArgs e)
        {
            RegisterWindow registerWindow = new RegisterWindow();
            registerWindow.Show();
            this.Close();
        }
        private void ForgotPassword_MouseDown(object sender, MouseButtonEventArgs e)
        {
            MessageBox.Show("This function is still under development.\nTry to guess the password :)",
                            "Info",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);
        }
    }
}
