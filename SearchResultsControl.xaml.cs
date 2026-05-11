using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using Messenger_Project.Models;

namespace Messenger_Project
{
    public partial class SearchResultsControl : UserControl
    {
        // Конструктор приймає список знайдених юзерів з БД
        public SearchResultsControl(List<User> foundUsers)
        {
            InitializeComponent();
            ResultsListBox.ItemsSource = foundUsers; 
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            // Закриваємо сторінку результатів і повертаємо чат
            if (Window.GetWindow(this) is MainWindow mainWindow)
            {
                mainWindow.MainArea.Content = null;
                mainWindow.ChatView.Visibility = Visibility.Visible;
            }
        }
    }
}