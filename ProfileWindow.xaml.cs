using System;
using System.Windows;

namespace Messenger_Project
{
    public partial class ProfileWindow : Window
    {
        public ProfileWindow(UserRecord user)
        {
            InitializeComponent();
            LoadProfile(user);
        }

        private void LoadProfile(UserRecord user)
        {
            DisplayUsername.Text = user.Username;
            UsernameText.Text = user.Username;
            BirthDateText.Text = string.IsNullOrEmpty(user.BirthDate) ? "Not specified" : user.BirthDate;
            BioText.Text = string.IsNullOrEmpty(user.Bio) ? "No bio yet" : user.Bio;
            MemberSinceText.Text = user.MemberSince.ToString("MMMM dd, yyyy", System.Globalization.CultureInfo.InvariantCulture);
        }

        private void EditProfile_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Edit profile — coming soon.", "Info",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
