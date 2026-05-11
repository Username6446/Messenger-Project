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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Messenger_Project
{
    /// <summary>
    /// Interaction logic for ProfileControl.xaml
    /// </summary>
    public partial class ProfileControl : UserControl
    {
        private UserRecord _currentUser;
        public ProfileControl()
        {
            InitializeComponent();
        }
        public ProfileControl(UserRecord user)
        {
            InitializeComponent();
            _currentUser = user;
            LoadProfile(user);
        }
        private void LoadProfile(UserRecord user)
        {
            DisplayUsername.Text = user.Username;
            UsernameText.Text = user.Username;
            MemberSinceText.Text = user.MemberSince.ToString("MMMM dd, yyyy", System.Globalization.CultureInfo.InvariantCulture);
        }

        private void EditProfile_Click(object sender, RoutedEventArgs e)
        {
            if (Window.GetWindow(this) is MainWindow mainWindow)
            {
                mainWindow.MainArea.Content = new EditProfileControl(_currentUser);
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
