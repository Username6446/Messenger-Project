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
        public EditProfileControl()
        {
            InitializeComponent();
        }
        public string UpdatedBio { get; private set; }

        public string UpdatedUsername { get; private set; }

        public DateTime? UpdatedBirthDate { get; private set; }


        private void Cancel_Button_Click(object sender, RoutedEventArgs e)
        {
            
        }
        private void Save_Button_Click(object sender, RoutedEventArgs e)
        {
            UpdatedUsername = UsernameBox.Text;
            UpdatedBirthDate = BirthDatePicker.SelectedDate;
            UpdatedBio = BioBox.Text;
            
        }

        private void Change_Avatar_Button_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
