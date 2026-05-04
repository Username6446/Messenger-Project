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
            // Для 
            //LoginWindow loginWindow = new LoginWindow();
            //loginWindow.Show();
            //this.Close();
            
            UserDatabase.AddUser("qwerty", "qwerty123");

            UserRecord user = UserDatabase.FindUser("qwerty", "qwerty123")!;

            MainWindow mainWindow = new MainWindow(user);
            mainWindow.Show();
            this.Close();
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
