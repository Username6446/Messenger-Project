using System.Windows;


namespace Messenger_Project
{

    public partial class EditProfileWindow : Window
    {
        public EditProfileWindow()
        {
            InitializeComponent();
        }

        public string UpdatedBio { get; private set; }

        public string UpdatedUsername { get; private set; }

        public DateTime? UpdatedBirthDate { get; private set; }


        private void Cancel_Button_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Save_Button_Click(object sender, RoutedEventArgs e)
        {
            UpdatedUsername = UsernameBox.Text;
            UpdatedBirthDate = BirthDatePicker.SelectedDate;
            UpdatedBio = BioBox.Text;
            this.DialogResult = true;
            this.Close();
        }

        private void Change_Avatar_Button_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
