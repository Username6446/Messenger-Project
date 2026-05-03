using System.Windows;

namespace Messenger_Project
{
    public partial class MainWindow : Window
    {
        private UserRecord _currentUser;

        public MainWindow(UserRecord user)
        {
            InitializeComponent();
            _currentUser = user;
        }

        private void OpenProfile_Click(object sender, RoutedEventArgs e)
        {
            ProfileWindow profileWindow = new ProfileWindow(_currentUser);
            profileWindow.Show();
        }
    }
}
