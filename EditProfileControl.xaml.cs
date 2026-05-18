using System;
using System.Windows;
using System.Windows.Controls;
using Messenger_Project.Models;
using Messenger_Project.Services;
using Microsoft.Win32;

namespace Messenger_Project
{
    public partial class EditProfileControl : UserControl
    {
        private User _currentUser;
        private ServerService? _serverService;
        private string? _selectedAvatarPath;
        private ProfileControl? _parentProfile;

        private const int MIN_USERNAME_LENGTH = 3;
        private const int MAX_USERNAME_LENGTH = 30;
        private const int MIN_BIO_LENGTH = 4;
        private const int MAX_BIO_LENGTH = 500;

        public EditProfileControl()
        {
            InitializeComponent();
        }

        public EditProfileControl(User user, ServerService? serverService = null, ProfileControl? parent = null)
        {
            InitializeComponent();
            _currentUser = user;
            _serverService = serverService ?? App.ServerService;
            _parentProfile = parent;

            UsernameBox.Text = _currentUser.Username ?? "";
            BioBox.Text = _currentUser.Bio ?? "";
            if (_currentUser.BirthDate.HasValue)
                BirthDatePicker.SelectedDate = _currentUser.BirthDate.Value;

            UpdateBioCharCount();
        }

        private void BioBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateBioCharCount();
        }

        private void UpdateBioCharCount()
        {
            int length = BioBox.Text.Length;
            BioCharCount.Text = length.ToString();

            if (length > MAX_BIO_LENGTH)
            {
                BioBox.Background = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromArgb(30, 255, 0, 0));
            }
            else
            {
                BioBox.Background = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Colors.White);
            }
        }

        private void Change_Avatar_Button_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "Image Files (*.jpg;*.jpeg;*.png)|*.jpg;*.jpeg;*.png",
                Title = "Select Avatar Image"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                _selectedAvatarPath = openFileDialog.FileName;

                try
                {
                    var bitmap = new System.Windows.Media.Imaging.BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(_selectedAvatarPath);
                    bitmap.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                    bitmap.EndInit();
                    AvatarImage.Source = bitmap;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"❌ Error loading image: {ex.Message}", "Error");
                    _selectedAvatarPath = null;
                }
            }
        }

        private bool ValidateInputs()
        {
            GeneralError.Text = "";
            UsernameError.Text = "";
            BioError.Text = "";

            string username = UsernameBox.Text.Trim();

            if (string.IsNullOrEmpty(username))
            {
                UsernameError.Text = $"Username required";
                return false;
            }

            if (username.Length < MIN_USERNAME_LENGTH)
            {
                UsernameError.Text = $"Min {MIN_USERNAME_LENGTH} characters";
                return false;
            }

            if (username.Length > MAX_USERNAME_LENGTH)
            {
                UsernameError.Text = $"Max {MAX_USERNAME_LENGTH} characters";
                return false;
            }

            string bio = BioBox.Text.Trim();
            if (!string.IsNullOrEmpty(bio))
            {
                if (bio.Length < MIN_BIO_LENGTH)
                {
                    BioError.Text = $"Min {MIN_BIO_LENGTH} characters";
                    return false;
                }

                if (bio.Length > MAX_BIO_LENGTH)
                {
                    BioError.Text = $"Max {MAX_BIO_LENGTH} characters";
                    return false;
                }
            }

            return true;
        }

        private async void Save_Button_Click(object sender, RoutedEventArgs e)
        {
            if (_serverService == null || !_serverService.IsConnected)
            {
                GeneralError.Text = "❌ Not connected to server";
                return;
            }

            if (!ValidateInputs())
                return;

            try
            {
                string username = UsernameBox.Text.Trim();
                string bio = BioBox.Text.Trim();
                DateTime? birthDate = BirthDatePicker.SelectedDate;

                var (success, error, data) = await _serverService.UpdateProfileAsync(
                    username: username,
                    bio: bio.Length == 0 ? null : bio,
                    birthDate: birthDate,
                    avatarPath: _selectedAvatarPath
                );

                if (success)
                {
                    MessageBox.Show("✅ Profile updated successfully!", "Success");

                    _currentUser.Username = username;
                    _currentUser.Bio = bio.Length == 0 ? null : bio;
                    _currentUser.BirthDate = birthDate;
                    if (!string.IsNullOrEmpty(_selectedAvatarPath))
                        _currentUser.AvatarPath = _selectedAvatarPath;

                    if (Window.GetWindow(this) is MainWindow mainWindow)
                    {
                        mainWindow.MainArea.Content = new ProfileControl(_currentUser);
                    }
                }
                else
                {
                    GeneralError.Text = $"❌ {error}";
                }
            }
            catch (Exception ex)
            {
                GeneralError.Text = $"❌ Error: {ex.Message}";
            }
        }

        private void Cancel_Button_Click(object sender, RoutedEventArgs e)
        {
            if (Window.GetWindow(this) is MainWindow mainWindow)
            {
                mainWindow.MainArea.Content = new ProfileControl(_currentUser);
            }
        }
    }
}