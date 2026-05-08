using System.Configuration;
using System.Data;
using System.Windows;
using Microsoft.EntityFrameworkCore;
using Messenger_Project.Data;
using System.IO;

namespace Messenger_Project
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            try
            {
                var basePath = AppContext.BaseDirectory;
                var appSettingsPath = Path.Combine(basePath, "appsettings.json");
                
                if (!File.Exists(appSettingsPath))
                {
                    MessageBox.Show($"appsettings.json not found at: {appSettingsPath}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var factory = new AppDbContextFactory();
                using (var context = factory.CreateDbContext(Array.Empty<string>()))
                {
                    // Ensure the database is created and migrations are applied
                    context.Database.Migrate();
                    MessageBox.Show("Database initialized successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                var message = $"Database initialization error:\n\n{ex.InnerException?.Message ?? ex.Message}";
                MessageBox.Show(message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                System.Diagnostics.Debug.WriteLine($"Full error: {ex}");
            }
        }
    }
}
