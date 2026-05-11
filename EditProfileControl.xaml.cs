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
using Messenger_Project.Models;
using Messenger_Project.Data;
namespace Messenger_Project
{
    /// <summary>
    /// Interaction logic for EditProfileControl.xaml
    /// </summary>
    public partial class EditProfileControl : UserControl
    {
        private User _currentUser;

        public EditProfileControl(User user)
        {
            InitializeComponent();
            _currentUser = user;

            UsernameBox.Text = _currentUser.Username;
            BioBox.Text = _currentUser.Bio;
            BirthDatePicker.SelectedDate = _currentUser.BirthDate;
        }
        public EditProfileControl()
        {
            InitializeComponent();
        }
        public string UpdatedBio { get; private set; }

        public string UpdatedUsername { get; private set; }

        public DateTime? UpdatedBirthDate { get; private set; }


        private void Cancel_Button_Click(object sender, RoutedEventArgs e)
        {
            if (Window.GetWindow(this) is MainWindow mainWindow)
            {
                mainWindow.MainArea.Content = new ProfileControl(_currentUser);
            }
        }
        private void Save_Button_Click(object sender, RoutedEventArgs e)
        {
            _currentUser.Username = UsernameBox.Text;
            _currentUser.Bio = BioBox.Text;
            _currentUser.BirthDate = BirthDatePicker.SelectedDate;

            using (var db = new AppDbContext())
            {
                db.Users.Update(_currentUser);
                db.SaveChanges();
            }

            if (Window.GetWindow(this) is MainWindow mainWindow)
            {
                mainWindow.MainArea.Content = new ProfileControl(_currentUser);
            }

        }

        private void Change_Avatar_Button_Click(object sender, RoutedEventArgs e)
        {
            // Логіка зміни аватарки
        }
    }
}
