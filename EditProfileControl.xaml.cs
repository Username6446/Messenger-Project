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
    /// Interaction logic for EditProfileControl.xaml
    /// </summary>
    public partial class EditProfileControl : UserControl
    {
        private UserRecord _currentUser;

        public EditProfileControl(UserRecord user)
        {
            InitializeComponent();
            _currentUser = user;

            // Завантажуємо поточні дані в поля вводу
            UsernameBox.Text = _currentUser.Username;
            // BirthDatePicker.SelectedDate = ... ; // Якщо є таке поле в базі
            // BioBox.Text = _currentUser.Bio; // Якщо є таке поле в базі
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
            // _currentUser.Bio = BioBox.Text; 

            // База данних
            //using (var db = new MessengerDbContext())
            //{
            //    db.Users.Update(_currentUser);
            //    db.SaveChanges();
            //}

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
